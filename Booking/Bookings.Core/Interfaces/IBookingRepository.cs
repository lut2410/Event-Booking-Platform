using Bookings.Core.Entities;

namespace Bookings.Core.Interfaces
{
    public interface IBookingRepository
    {
        IEnumerable<Booking> GetAllBookings();
        Booking GetBookingById(Guid id);
        void AddBooking(Booking booking);
    }
}
