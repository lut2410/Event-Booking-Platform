using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bookings.Application.DTOs;
using Bookings.Application.Interfaces;
using Bookings.Core.Entities;
using Bookings.Core.Interfaces.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Wrap;

namespace Bookings.Application.Services
{
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly ISeatRepository _seatRepository;
        private readonly IPaymentService _paymentService;
        private readonly IRedisSeatReservationService _reservationCacheService;
        private readonly IRedisFraudDetectionService _fraudDetectionService;
        private readonly ILogger<BookingService> _logger;
        private readonly AsyncPolicyWrap _policyWrap;
        private readonly TimeSpan _reservationTime;

        private readonly ConcurrentDictionary<Guid, int> _failedReservations = new();
        private readonly ConcurrentDictionary<Guid, int> _failedPayments = new();
        private readonly int _maxFailedAttempts = 5;

        public BookingService(
            IBookingRepository bookingRepository,
            ISeatRepository seatRepository,
            IPaymentService paymentService,
            IRedisSeatReservationService reservationCacheService,
            IRedisFraudDetectionService fraudDetectionService,
            IConfiguration configuration,
            ILogger<BookingService> logger)
        {
            _bookingRepository = bookingRepository;
            _seatRepository = seatRepository;
            _paymentService = paymentService;
            _reservationCacheService = reservationCacheService;
            _fraudDetectionService = fraudDetectionService;
            _logger = logger;

            _policyWrap = ConfigurePolicies();

            var reservationMinutes = configuration.GetValue<int>("SeatReservation:TimeInMinutes", 10);
            _reservationTime = TimeSpan.FromMinutes(reservationMinutes);
            _logger.LogInformation("Initialized BookingService with reservation time: {ReservationTime}", _reservationTime);
        }

        private AsyncPolicyWrap ConfigurePolicies()
        {
            var retryPolicy = Policy
                .Handle<Exception>()
                .RetryAsync(3, (exception, retryCount) =>
                {
                    _logger.LogWarning("Retry {RetryCount} due to: {ExceptionMessage}", retryCount, exception.Message);
                });

            var circuitBreakerPolicy = Policy
                .Handle<Exception>()
                .CircuitBreakerAsync(
                    exceptionsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromMinutes(1),
                    onBreak: (ex, duration) =>
                    {
                        _logger.LogWarning("Circuit breaker opened for {Duration} due to: {ExceptionMessage}", duration, ex.Message);
                    },
                    onReset: () => _logger.LogInformation("Circuit breaker reset."),
                    onHalfOpen: () => _logger.LogInformation("Circuit breaker half-open; testing service.")
                );

            return Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);
        }

        public async Task<IEnumerable<BookingDTO>> GetAllAsync()
        {
            _logger.LogInformation("Fetching all bookings.");
            var bookings = await _bookingRepository.GetAllAsync();
            _logger.LogInformation("Successfully retrieved {Count} bookings.", bookings.Count());
            return bookings.Select(b => new BookingDTO
            {
                Id = b.Id,
                BookingDate = b.BookingDate,
                PaymentStatus = b.PaymentStatus,
                EventId = b.EventId,
                UserId = b.UserId,
                Seats = b.BookingSeats.Select(bs => new SeatDTO
                {
                    Id = bs.Seat.Id,
                    SeatNumber = bs.Seat.SeatNumber,
                    Status = bs.Seat.Status
                }).ToList()
            }).ToList();
        }

        public async Task<BookingDTO> GetByIdAsync(Guid id)
        {
            _logger.LogInformation("Fetching booking details for BookingId: {BookingId}", id);
            var booking = await _bookingRepository.GetByIdAsync(id);

            if (booking == null)
            {
                _logger.LogWarning("No booking found with BookingId: {BookingId}", id);
                return null;
            }

            _logger.LogInformation("Successfully retrieved booking with BookingId: {BookingId}", id);
            return new BookingDTO
            {
                Id = booking.Id,
                BookingDate = booking.BookingDate,
                PaymentStatus = booking.PaymentStatus,
                EventId = booking.EventId,
                UserId = booking.UserId,
                Seats = booking.BookingSeats.Select(bs => new SeatDTO
                {
                    Id = bs.Seat.Id,
                    SeatNumber = bs.Seat.SeatNumber,
                    Status = bs.Seat.Status
                }).ToList()
            };
        }

        public async Task<ReservationResult> ReserveSeatsAsync(Guid eventId, Guid userId, List<Guid> seatIds)
        {
            _logger.LogInformation("Attempting to reserve seats for EventId: {EventId}, UserId: {UserId}", eventId, userId);

            if (await _fraudDetectionService.IsUserBlockedAsync(userId))
            {
                return new ReservationResult { Success = false, Message = "Reservation blocked due to suspected fraud." };
            }

            // Execute reserve operation with circuit breaker and retry policies
            return await _policyWrap.ExecuteAsync(async () =>
            {
                var redisReservationSuccess = await _reservationCacheService.TryReserveSeatsAsync(eventId, userId, seatIds, _reservationTime);
                if (!redisReservationSuccess)
                {
                    _logger.LogWarning("Redis reservation failed for EventId: {EventId}, UserId: {UserId}", eventId, userId);
                    await _fraudDetectionService.RecordFailedAttemptAsync(userId);
                    return new ReservationResult { Success = false, Message = "Some seats are no longer available." };
                }

                var seats = await _seatRepository.GetSeatsByIdsAsync(seatIds, noTracking: true);
                if (seats.Count != seatIds.Count || seats.Any(seat => seat.EventId != eventId || seat.Status != SeatStatus.Available))
                {
                    await _reservationCacheService.ReleaseSeatsAsync(eventId, userId, seatIds);
                    await _fraudDetectionService.RecordFailedAttemptAsync(userId);
                    return new ReservationResult { Success = false, Message = "One or more seats are no longer available." };
                }

                var reservationExpiresAt = DateTimeOffset.UtcNow.Add(_reservationTime);
                foreach (var seat in seats)
                {
                    seat.Status = SeatStatus.Reserved;
                    seat.ReservationExpiresAt = reservationExpiresAt;
                }

                var booking = new Booking
                {
                    Id = Guid.NewGuid(),
                    EventId = eventId,
                    UserId = userId,
                    PaymentStatus = PaymentStatus.Pending,
                    BookingDate = DateTimeOffset.Now,
                    BookingSeats = seats.Select(seat => new BookingSeat { SeatId = seat.Id }).ToList()
                };

                await _bookingRepository.AddAsync(booking);
                await _seatRepository.UpdateSeatsAsync(seats);

                await _fraudDetectionService.ClearFailedAttemptsAsync(userId);

                _logger.LogInformation("Seats successfully reserved for BookingId: {BookingId} until {ExpiresAt}", booking.Id, reservationExpiresAt);
                return new ReservationResult { Success = true, Message = "Seats reserved successfully.", CreatedBookingId = booking.Id, ReservationExpiresAt = reservationExpiresAt };
            });
        }

        public async Task<PaymentResult> ConfirmPaymentAsync(Guid bookingId, Guid userId, PaymentRequest paymentRequest)
        {
            _logger.LogInformation("Attempting to confirm payment for BookingId: {BookingId}, UserId: {UserId}", bookingId, userId);

            // Check if user is blocked due to fraud detection
            if (await _fraudDetectionService.IsUserBlockedAsync(userId))
            {
                _logger.LogWarning("Payment blocked for UserId: {UserId} due to suspected fraud", userId);
                return new PaymentResult { Success = false, Message = "Payment blocked due to suspected fraud." };
            }

            return await _policyWrap.ExecuteAsync(async () =>
            {
                var booking = await _bookingRepository.GetByIdAsync(bookingId, noTracking: true);
                if (booking == null || booking.UserId != userId || booking.BookingSeats.Any(bs => bs.Seat.Status != SeatStatus.Reserved))
                {
                    _logger.LogWarning("Payment confirmation failed for BookingId: {BookingId} due to reservation issues.", bookingId);
                    await _fraudDetectionService.RecordFailedAttemptAsync(userId); // Log as a failed attempt
                    return new PaymentResult { Success = false, Message = "Seats are no longer reserved." };
                }

                var paymentResult = await _paymentService.ProcessPaymentAsync(paymentRequest);
                if (!paymentResult.Success)
                {
                    await _fraudDetectionService.RecordFailedAttemptAsync(userId); // Log failed payment attempt
                    await _reservationCacheService.ReleaseSeatsAsync(booking.EventId, userId, booking.BookingSeats.Select(bs => bs.SeatId).ToList());
                    return paymentResult;
                }

                // Clear any failed attempts after successful payment
                await _fraudDetectionService.ClearFailedAttemptsAsync(userId);

                // Update booking status and finalize seat reservation
                booking.PaymentStatus = PaymentStatus.Paid;
                booking.ChargedDate = DateTimeOffset.Now;
                booking.PaymentIntentId = paymentResult.PaymentIntentId;
                foreach (var bookingSeat in booking.BookingSeats)
                {
                    bookingSeat.Seat.Status = SeatStatus.Booked;
                    bookingSeat.Seat.ReservationExpiresAt = null;
                }

                await _bookingRepository.UpdateAsync(booking);
                await _reservationCacheService.ConfirmReservationAsync(booking.EventId, booking.BookingSeats.Select(bs => bs.SeatId).ToList());

                _logger.LogInformation("Payment confirmed for BookingId: {BookingId}, UserId: {UserId}", bookingId, userId);
                return new PaymentResult { Success = true, Message = "Payment successful. Seats confirmed." };
            });
        }

        public async Task<PaymentResult> RequestRefundAsync(Guid bookingId, RefundRequest refundRequest)
        {
            _logger.LogInformation("Processing refund for BookingId: {BookingId}", bookingId);

            var booking = await _bookingRepository.GetByIdAsync(bookingId, noTracking: true);

            if (booking == null || booking.PaymentStatus != PaymentStatus.Paid)
            {
                _logger.LogWarning("Refund eligibility check failed for BookingId: {BookingId}. Status: {Status}", bookingId, booking?.PaymentStatus);
                return new PaymentResult { Success = false, Message = "Booking not eligible for refund." };
            }

            if (!IsRefundEligible(booking))
            {
                _logger.LogWarning("Refund denied due to policy for BookingId: {BookingId}", bookingId);
                return new PaymentResult { Success = false, Message = "Booking outside the refund policy." };
            }

            _logger.LogInformation("Initiating payment refund process for BookingId: {BookingId}", bookingId);
            var refundStatus = await _paymentService.ProcessRefundAsync(refundRequest);

            if (refundStatus == "succeeded")
            {
                booking.PaymentStatus = PaymentStatus.Refunded;
                await _bookingRepository.UpdateAsync(booking);
                _logger.LogInformation("Refund successful for BookingId: {BookingId}", bookingId);
                return new PaymentResult { Success = true, Message = "Refund successful." };
            }

            _logger.LogError("Refund failed for BookingId: {BookingId}. Status: {RefundStatus}", bookingId, refundStatus);
            return new PaymentResult { Success = false, Message = $"Refund failed: {refundStatus}" };
        }

        public async Task<PaymentResult> SelfRequestRefundAsync(Guid bookingId)
        {
            _logger.LogInformation("Processing self-initiated refund for BookingId: {BookingId}", bookingId);

            var booking = await _bookingRepository.GetByIdAsync(bookingId, noTracking: true);
            if (booking == null)
            {
                _logger.LogWarning("Refund request failed. BookingId: {BookingId} not found or access denied.", bookingId);
                return new PaymentResult { Success = false, Message = "Booking not found or access denied." };
            }

            if (!IsRefundEligible(booking))
            {
                _logger.LogWarning("Refund request denied due to expired refund period for BookingId: {BookingId}", bookingId);
                return new PaymentResult { Success = false, Message = "Refund period has expired." };
            }

            if (booking.PaymentStatus != PaymentStatus.Paid)
            {
                _logger.LogWarning("Refund request denied. BookingId: {BookingId} is not eligible for refund. Current status: {Status}", bookingId, booking.PaymentStatus);
                return new PaymentResult { Success = false, Message = "Booking is not eligible for a refund." };
            }

            _logger.LogInformation("Initiating payment refund process for BookingId: {BookingId}", bookingId);
            var refundRequest = new RefundRequest
            {
                PaymentIntentId = booking.PaymentIntentId,
                Reason = "requested_by_customer"
            };

            var refundStatus = await _paymentService.ProcessRefundAsync(refundRequest);
            if (refundStatus == "succeeded")
            {
                booking.PaymentStatus = PaymentStatus.Refunded;
                await _bookingRepository.UpdateAsync(booking);
                _logger.LogInformation("Self-requested refund successful for BookingId: {BookingId}", bookingId);
                return new PaymentResult { Success = true, Message = "Refund successful." };
            }

            _logger.LogError("Self-requested refund failed for BookingId: {BookingId}. Status: {RefundStatus}", bookingId, refundStatus);
            return new PaymentResult { Success = false, Message = $"Refund failed: {refundStatus}" };
        }

        private bool IsRefundEligible(Booking booking)
        {
            return true;
        }

    }
}
