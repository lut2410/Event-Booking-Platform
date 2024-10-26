using Bookings.Core.Entities;
using Bookings.Core.Interfaces.Repositories;
using Bookings.Core.Interfaces.Services;

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

        public async Task<IEnumerable<Booking>> GetAllAsync() => await _bookingRepository.GetAllAsync();
        public Task<Booking> GetByIdAsync(Guid id) => _bookingRepository.GetByIdAsync(id);
        public async Task<Booking> CreateBookingAsync(Guid eventId, Guid userId, List<Guid> seatIds)
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
                BookingSeats = seats.Select(seat => new BookingSeat
                {
                    SeatId = seat.Id
                }).ToList()
            };

            // Save the booking and update seat status in the database
            await _bookingRepository.AddAsync(booking);
            await _seatRepository.UpdateSeatsAsync(seats);

            return booking;
        }
    }
}
