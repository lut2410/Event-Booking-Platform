using Bookings.Core.Entities;
using Bookings.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Bookings.Infrastructure.Repositories
{
    public class SeatRepository : ISeatRepository
    {
        private readonly AppDbContext _dbContext;

        public SeatRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<Seat>> GetSeatsByIdsAsync(List<Guid> seatIds, bool noTracking = false)
        {
            var query = _dbContext.Seats
                .Where(seat => seatIds.Contains(seat.Id));
            if (noTracking)
            {
                query = query.AsNoTracking();
            }
            return await query.ToListAsync();
        }

        public async Task UpdateSeatsAsync(List<Seat> seats)
        {
            _dbContext.Seats.UpdateRange(seats);
            await _dbContext.SaveChangesAsync();
        }

        public async Task ReloadEntryAsync(Seat seat)
        {
            await _dbContext.Entry(seat).ReloadAsync();
        }
        public void MarkEntryModified(Seat seat)
        {
            _dbContext.Entry(seat).State = EntityState.Modified;
        }
    }
}
