using Bookings.Core.Entities;
using Bookings.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Bookings.Infrastructure.Repositories
{
    public class BookingRepository : IBookingRepository
    {
        private readonly AppDbContext _dbContext;

        public BookingRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<Booking>> GetAllAsync()
        {
            return await _dbContext.Bookings
                .Include(b => b.BookingSeats)
                .ThenInclude(bs => bs.Seat)
                .ToListAsync();
        }

        public async Task<Booking> GetByIdAsync(Guid bookingId, bool noTracking = false)
        {
            var query = _dbContext.Bookings
                .Include(b => b.BookingSeats)
                .ThenInclude(bs => bs.Seat)
                .Where(b => b.Id == bookingId);

            if (noTracking)
            {
                query = query.AsNoTracking();
            }

            return await query.FirstOrDefaultAsync();
        }

        public async Task AddAsync(Booking booking)
        {
            await _dbContext.Bookings.AddAsync(booking);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateAsync(Booking booking)
        {
            _dbContext.Bookings.Update(booking);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid bookingId)
        {
            var booking = await _dbContext.Bookings.FindAsync(bookingId);
            if (booking != null)
            {
                _dbContext.Bookings.Remove(booking);
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task ReloadEntryAsync(Booking booking)
        {
            await _dbContext.Entry(booking).ReloadAsync();
        }
    }
}
