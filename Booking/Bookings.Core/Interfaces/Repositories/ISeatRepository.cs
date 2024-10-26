using Bookings.Core.Entities;

namespace Bookings.Core.Interfaces.Repositories
{
    public interface ISeatRepository
    {
        Task<List<Seat>> GetSeatsByIdsAsync(List<Guid> seatIds);
        Task UpdateSeatsAsync(List<Seat> seats);
    }
}
