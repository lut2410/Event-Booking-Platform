using Bookings.Core.Entities;

namespace Bookings.Core.Interfaces.Repositories
{
    public interface ISeatRepository
    {
        Task<List<Seat>> GetSeatsByIdsAsync(List<Guid> seatIds, bool noTracking = false);
        Task UpdateSeatsAsync(List<Seat> seats);
        Task ReloadEntryAsync(Seat seat);
        void MarkEntryModified(Seat seat);
        Task<List<Seat>> GetExpiredReservationsAsync(DateTimeOffset currentDateTime);
    }
}
