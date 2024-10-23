using Bookings.Core.Entities;
using Bookings.Core.Interfaces;

namespace Bookings.Application.Services
{
    public class BookingService
    {
        private readonly IBookingRepository _bookingRepository;

        public BookingService(IBookingRepository bookingRepository)
        {
            _bookingRepository = bookingRepository;
        }

        public IEnumerable<Booking> GetAllBookings() => _bookingRepository.GetAllBookings();
        public Booking GetBookingById(int id) => _bookingRepository.GetBookingById(id);
        public void AddBooking(Booking booking) => _bookingRepository.AddBooking(booking);
    }
}
