using Bookings.Core.Entities;

namespace Bookings.Core.Interfaces.Services
{
    public interface IBookingService
    {
        Task<IEnumerable<Booking>> GetAllAsync();
        Task<Booking> GetByIdAsync(Guid bookingId);
        Task<Booking> CreateBookingAsync(Guid eventId, Guid userId, List<Guid> seatIds);
    }
}
