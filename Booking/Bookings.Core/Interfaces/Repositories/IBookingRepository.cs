using Bookings.Core.Entities;

namespace Bookings.Core.Interfaces.Repositories
{
    public interface IBookingRepository
    {
        Task<IEnumerable<Booking>> GetAllAsync();
        Task<Booking> GetByIdAsync(Guid bookingId, bool noTracking = false);
        Task AddAsync(Booking booking);
        Task UpdateAsync(Booking booking);
        Task DeleteAsync(Guid bookingId);
        Task ReloadEntryAsync(Booking booking);
    }
}
