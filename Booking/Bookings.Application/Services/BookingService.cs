using Bookings.Application.DTOs;
using Bookings.Application.Interfaces;
using Bookings.Core.Entities;
using Bookings.Core.Interfaces.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Bookings.Application.Services
{
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly ISeatRepository _seatRepository;
        private readonly IPaymentService _paymentService;
        private readonly IRedisSeatReservationService _reservationCacheService;
        private readonly ILogger<BookingService> _logger;
        private readonly TimeSpan _reservationTime;

        public BookingService(
            IBookingRepository bookingRepository,
            ISeatRepository seatRepository,
            IPaymentService paymentService,
            IRedisSeatReservationService reservationCacheService,
            IConfiguration configuration,
            ILogger<BookingService> logger)
        {
            _bookingRepository = bookingRepository;
            _seatRepository = seatRepository;
            _paymentService = paymentService;
            _reservationCacheService = reservationCacheService;
            _logger = logger;

            var reservationMinutes = configuration.GetValue<int>("SeatReservation:TimeInMinutes", 10);
            _reservationTime = TimeSpan.FromMinutes(reservationMinutes);
            _logger.LogInformation("Initialized BookingService with reservation time: {ReservationTime}", _reservationTime);
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

            var redisReservationSuccess = await _reservationCacheService.TryReserveSeatsAsync(eventId, userId, seatIds, _reservationTime);
            if (!redisReservationSuccess)
            {
                _logger.LogWarning("Redis reservation failed for EventId: {EventId}, UserId: {UserId}", eventId, userId);
                return new ReservationResult { Success = false, Message = "Some seats are no longer available." };
            }

            int maxRetries = 3;
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    var seats = await _seatRepository.GetSeatsByIdsAsync(seatIds, noTracking: true);
                    if (seats.Count != seatIds.Count || seats.Any(seat => seat.EventId != eventId || seat.Status != SeatStatus.Available))
                    {
                        _logger.LogWarning("Seat reservation failed: some seats are unavailable for EventId: {EventId}, UserId: {UserId}", eventId, userId);
                        await _reservationCacheService.ReleaseSeatsAsync(eventId, userId, seatIds);
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
                        BookingDate = DateTimeOffset.UtcNow,
                        BookingSeats = seats.Select(seat => new BookingSeat { SeatId = seat.Id }).ToList()
                    };

                    await _bookingRepository.AddAsync(booking);
                    await _seatRepository.UpdateSeatsAsync(seats);

                    _logger.LogInformation("Seats successfully reserved for BookingId: {BookingId} until {ExpiresAt}", booking.Id, reservationExpiresAt);
                    return new ReservationResult { Success = true, Message = "Seats reserved successfully.", CreatedBookingId = booking.Id, ReservationExpiresAt = reservationExpiresAt };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reserving seats on attempt {Attempt} for EventId: {EventId}, UserId: {UserId}", attempt + 1, eventId, userId);
                    if (attempt == maxRetries - 1)
                    {
                        await _reservationCacheService.ReleaseSeatsAsync(eventId, userId, seatIds);
                        throw;
                    }
                }
            }
            _logger.LogWarning("Unable to reserve seats after {MaxRetries} attempts for EventId: {EventId}, UserId: {UserId}", maxRetries, eventId, userId);
            return new ReservationResult { Success = false, Message = "Unable to reserve seats due to concurrent updates." };
        }

        public async Task<PaymentResult> ConfirmPaymentAsync(Guid bookingId, Guid userId, PaymentRequest paymentRequest)
        {
            _logger.LogInformation("Confirming payment for BookingId: {BookingId}, UserId: {UserId}", bookingId, userId);

            int maxRetries = 3;
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    var booking = await _bookingRepository.GetByIdAsync(bookingId, noTracking: true);
                    if (booking == null || booking.UserId != userId || booking.BookingSeats.Any(bs => bs.Seat.Status != SeatStatus.Reserved))
                    {
                        _logger.LogWarning("Payment confirmation failed due to reservation issues for BookingId: {BookingId}", bookingId);
                        return new PaymentResult { Success = false, Message = "Seats are no longer reserved." };
                    }

                    var paymentResult = await _paymentService.ProcessPaymentAsync(paymentRequest);

                    if (!paymentResult.Success)
                    {
                        _logger.LogWarning("Payment failed for BookingId: {BookingId}, releasing reserved seats", bookingId);
                        await _reservationCacheService.ReleaseSeatsAsync(booking.EventId, userId, booking.BookingSeats.Select(bs => bs.SeatId).ToList());
                        return paymentResult;
                    }

                    booking.PaymentStatus = PaymentStatus.Paid;
                    booking.ChargedDate = DateTimeOffset.UtcNow;
                    booking.PaymentIntentId = paymentResult.PaymentIntentId;
                    foreach (var bookingSeat in booking.BookingSeats)
                    {
                        bookingSeat.Seat.Status = SeatStatus.Booked;
                        bookingSeat.Seat.ReservationExpiresAt = null;
                    }

                    await _bookingRepository.UpdateAsync(booking);
                    await _reservationCacheService.ConfirmReservationAsync(booking.EventId, booking.BookingSeats.Select(bs => bs.SeatId).ToList());

                    _logger.LogInformation("Payment confirmed for BookingId: {BookingId}", bookingId);
                    return new PaymentResult { Success = true, Message = "Payment successful. Seats confirmed." };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error confirming payment on attempt {Attempt} for BookingId: {BookingId}, UserId: {UserId}", attempt + 1, bookingId, userId);
                    if (attempt == maxRetries - 1) throw;
                }
            }

            _logger.LogWarning("Unable to confirm payment after {MaxRetries} attempts for BookingId: {BookingId}", maxRetries, bookingId);
            return new PaymentResult { Success = false, Message = "Unable to confirm payment due to concurrent updates." };
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
