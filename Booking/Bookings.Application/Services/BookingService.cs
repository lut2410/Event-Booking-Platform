﻿using Bookings.Application.DTOs;
using Bookings.Application.Interfaces;
using Bookings.Core.Entities;
using Bookings.Core.Interfaces.Repositories;
using Stripe;
using System.Net.Sockets;

namespace Bookings.Application.Services
{
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly ISeatRepository _seatRepository;
        private readonly IPaymentService _paymentService;

        public BookingService(IBookingRepository bookingRepository, ISeatRepository seatRepository, IPaymentService paymentService)
        {
            _bookingRepository = bookingRepository;
            _seatRepository = seatRepository;
            _paymentService = paymentService;
        }

        public async Task<IEnumerable<BookingDTO>> GetAllAsync()
        {
            var bookings = await _bookingRepository.GetAllAsync();

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
            var booking = await _bookingRepository.GetByIdAsync(id);

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
            var seats = await _seatRepository.GetSeatsByIdsAsync(seatIds);
            if (seats.Count != seatIds.Count || seats.Any(seat => seat.EventId != eventId || seat.Status != SeatStatus.Available))
                return new ReservationResult { Success = false, Message = "One or more seats are no longer available." };
            var reservationExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10);
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
                BookingSeats = seats.Select(seat => new BookingSeat
                {
                    SeatId = seat.Id
                }
                ).ToList()
            };

            await _bookingRepository.AddAsync(booking);
            await _seatRepository.UpdateSeatsAsync(seats);

            return new ReservationResult { Success = true, Message = "Seats reserved successfully.", CreatedBookingId = booking.Id, ReservationExpiresAt = reservationExpiresAt };
        }

        public async Task<PaymentResult> ConfirmPaymentAsync(Guid bookingId, Guid userId, PaymentRequest paymentRequest)
        {
            var booking = await _bookingRepository.GetByIdAsync(bookingId);
            if (booking == null || booking.UserId != userId || booking.BookingSeats.Any(bs => bs.Seat.Status != SeatStatus.Reserved))
                return new PaymentResult { Success = false, Message = "Seats are no longer reserved." };

            var paymentResult = await _paymentService.ProcessPaymentAsync(paymentRequest);

            if (!paymentResult.Success)
            {
                return paymentResult;
            }

            booking.PaymentStatus = PaymentStatus.Paid;
            booking.ChargedDate = DateTimeOffset.Now;
            booking.PaymentIntentId = paymentResult.PaymentIntentId;
            foreach (var bookingSeat in booking.BookingSeats)
            {
                bookingSeat.Seat.Status = SeatStatus.Booked;
                bookingSeat.Seat.ReservationExpiresAt = null;
            }

            await _bookingRepository.UpdateAsync(booking);

            return new PaymentResult { Success = true, Message = "Payment successful. Seats confirmed." };
        }

        public async Task<PaymentResult> RequestRefundAsync(Guid bookingId, RefundRequest refundRequest)
        {
            var booking = await _bookingRepository.GetByIdAsync(bookingId);

            if (booking == null || booking.PaymentStatus != PaymentStatus.Paid)
                return new PaymentResult { Success = false, Message = "Booking not eligible for refund." };

            if (!IsRefundEligible(booking))
                return new PaymentResult { Success = false, Message = "Booking outside the refund policy." };

            var refundStatus = await _paymentService.ProcessRefundAsync(refundRequest);

            if (refundStatus == "succeeded")
            {
                booking.PaymentStatus = PaymentStatus.Refunded;
                await _bookingRepository.UpdateAsync(booking);
                return new PaymentResult { Success = true, Message = "Refund successful." };
            }

            return new PaymentResult { Success = false, Message = $"Refund failed: {refundStatus}" };
        }

        public async Task<PaymentResult> SelfRequestRefundAsync(Guid bookingId)
        {
            var booking = await _bookingRepository.GetByIdAsync(bookingId);
            if (booking == null)
            {
                return new PaymentResult { Success = false, Message = "Booking not found or access denied." };
            }

            if (!IsRefundEligible(booking))
            {
                return new PaymentResult { Success = false, Message = "Refund period has expired." };
            }

            if (booking.PaymentStatus != PaymentStatus.Paid)
            {
                return new PaymentResult { Success = false, Message = "Booking is not eligible for a refund." };
            }

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

                return new PaymentResult { Success = true, Message = "Refund successful." };
            }

            return new PaymentResult { Success = false, Message = $"Refund failed: {refundStatus}" };
        }

        private bool IsRefundEligible(Booking booking)
        {
            return true;
        }

    }
}
