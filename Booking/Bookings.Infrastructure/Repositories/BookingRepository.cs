using Bookings.Core.Entities;
using Bookings.Core.Interfaces;

namespace Bookings.Infrastructure.Repositories
{
    public class BookingRepository : IBookingRepository
    {
        private readonly List<Booking> _bookings = new();

        public IEnumerable<Booking> GetAllBookings() => _bookings;
        public Booking GetBookingById(int id) => _bookings.FirstOrDefault(b => b.Id == id);
        public void AddBooking(Booking booking) => _bookings.Add(booking);
    }
}
