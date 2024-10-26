using Bookings.Application.DTOs;
using Bookings.Application.Interfaces;
using Bookings.Core.Entities;
using Bookings.Core.Interfaces.Repositories;

namespace Bookings.Application.Services
{
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly ISeatRepository _seatRepository;

        public BookingService(IBookingRepository bookingRepository, ISeatRepository seatRepository)
        {
            _bookingRepository = bookingRepository;
            _seatRepository = seatRepository;
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
        public async Task<BookingDTO> CreateBookingAsync(Guid eventId, Guid userId, List<Guid> seatIds)
        {
            var seats = await _seatRepository.GetSeatsByIdsAsync(seatIds);
            if (seats.Count != seatIds.Count || seats.Any(seat => seat.EventId != eventId || seat.Status != SeatStatus.Available))
                throw new Exception("One or more seats are not available for this event.");

            foreach (var seat in seats)
            {
                seat.Status = SeatStatus.Booked;
                seat.ReservationExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10);
            }

            // Create the booking and link each seat through BookingSeat
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

            // Save the booking and update seat status in the database
            await _bookingRepository.AddAsync(booking);
            await _seatRepository.UpdateSeatsAsync(seats);

            return new BookingDTO
            {
                Id = booking.Id,
                EventId = eventId,
                UserId = userId,
                BookingDate = booking.BookingDate,
                PaymentStatus = booking.PaymentStatus,
                Seats = booking.BookingSeats.Select(bs => new SeatDTO
                {
                    Id = bs.Seat.Id,
                    SeatNumber = bs.Seat.SeatNumber,
                    Status = bs.Seat.Status
                }).ToList()
            }; ;
        }
    }
}
