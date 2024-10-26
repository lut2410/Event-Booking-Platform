using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bookings.Core.Entities;
using Bookings.Infrastructure;
using Bookings.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bookings.Tests.Infrastructure.Repositories
{
    public class SeatRepositoryTests
    {
        private readonly DbContextOptions<AppDbContext> _dbContextOptions;

        public SeatRepositoryTests()
        {
            _dbContextOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"SeatDb_{Guid.NewGuid()}")
                .Options;
        }

        private async Task SeedDataAsync(AppDbContext dbContext)
        {
            var seats = new List<Seat>
            {
                new Seat { Id = Guid.NewGuid(), SeatNumber = "A1", Status = SeatStatus.Available,
                RowVersion = new byte[8]
                },
                new Seat { Id = Guid.NewGuid(), SeatNumber = "A2", Status = SeatStatus.Available,
                RowVersion = new byte[8]
                },
                new Seat { Id = Guid.NewGuid(), SeatNumber = "B1", Status = SeatStatus.Reserved,
                RowVersion = new byte[8]
                }
            };
            await dbContext.Seats.AddRangeAsync(seats);
            await dbContext.SaveChangesAsync();
        }

        [Fact]
        public async Task GetSeatsByIdsAsync_ShouldReturnMatchingSeats()
        {
            using var dbContext = new AppDbContext(_dbContextOptions);
            var repository = new SeatRepository(dbContext);

            await SeedDataAsync(dbContext);
            var existingSeatIds = dbContext.Seats.Select(s => s.Id).Take(2).ToList();

            var result = await repository.GetSeatsByIdsAsync(existingSeatIds);

            Assert.Equal(2, result.Count);
            Assert.True(result.All(seat => existingSeatIds.Contains(seat.Id)));
        }

        [Fact]
        public async Task GetSeatsByIdsAsync_ShouldReturnUntrackedSeats_WhenNoTrackingIsTrue()
        {
            using var dbContext = new AppDbContext(_dbContextOptions);
            var repository = new SeatRepository(dbContext);

            await SeedDataAsync(dbContext);
            var seatId = dbContext.Seats.First().Id;

            var result = await repository.GetSeatsByIdsAsync(new List<Guid> { seatId }, noTracking: true);

            Assert.Single(result);
            Assert.False(dbContext.Entry(result.First()).IsKeySet && dbContext.Entry(result.First()).State == EntityState.Unchanged);
        }

        [Fact]
        public async Task UpdateSeatsAsync_ShouldUpdateSeatStatus()
        {
            using var dbContext = new AppDbContext(_dbContextOptions);
            var repository = new SeatRepository(dbContext);

            await SeedDataAsync(dbContext);
            var seatToUpdate = dbContext.Seats.First();
            seatToUpdate.Status = SeatStatus.Booked;

            await repository.UpdateSeatsAsync(new List<Seat> { seatToUpdate });

            var updatedSeat = await dbContext.Seats.FindAsync(seatToUpdate.Id);
            Assert.Equal(SeatStatus.Booked, updatedSeat.Status);
        }

        [Fact]
        public async Task ReloadEntryAsync_ShouldReloadSeat()
        {
            using var dbContext = new AppDbContext(_dbContextOptions);
            var repository = new SeatRepository(dbContext);

            await SeedDataAsync(dbContext);
            var seat = dbContext.Seats.First();
            seat.Status = SeatStatus.Booked;
            await dbContext.SaveChangesAsync();

            seat.Status = SeatStatus.Available;
            await repository.ReloadEntryAsync(seat);

            Assert.Equal(SeatStatus.Booked, seat.Status);
        }

        [Fact]
        public async Task MarkEntryModified_ShouldMarkSeatAsModified()
        {
            using var dbContext = new AppDbContext(_dbContextOptions);
            var repository = new SeatRepository(dbContext);

            await SeedDataAsync(dbContext);
            var seat = dbContext.Seats.First();
            seat.Status = SeatStatus.Reserved;

            repository.MarkEntryModified(seat);
            await dbContext.SaveChangesAsync();

            var modifiedSeat = await dbContext.Seats.FindAsync(seat.Id);
            Assert.Equal(SeatStatus.Reserved, modifiedSeat.Status);
        }
    }
}
