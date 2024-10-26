using Bookings.Core.Entities;
using Bookings.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Bookings.Infrastructure.Repositories
{
    public class SeatRepository : ISeatRepository
    {
        private readonly AppDbContext _dbContext;

        public SeatRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<Seat>> GetSeatsByIdsAsync(List<Guid> seatIds)
        {
            return await _dbContext.Seats
                .Where(seat => seatIds.Contains(seat.Id))
                .ToListAsync();
        }

        public async Task UpdateSeatsAsync(List<Seat> seats)
        {
            _dbContext.Seats.UpdateRange(seats);
            await _dbContext.SaveChangesAsync();
        }
    }
}
