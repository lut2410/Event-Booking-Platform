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
    public class BookingRepositoryTests
    {
        private readonly DbContextOptions<AppDbContext> _dbContextOptions;

        public BookingRepositoryTests()
        {
            _dbContextOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"BookingDb_{Guid.NewGuid()}")
                .Options;
        }

        private async Task SeedDataAsync(AppDbContext dbContext)
        {
            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                EventId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                BookingDate = DateTimeOffset.UtcNow,
                PaymentStatus = PaymentStatus.Pending,
                RowVersion = new byte[8],
                BookingSeats = new List<BookingSeat>
                {
                    new BookingSeat
                    {
                        Seat = new Seat
                        {
                            Id = Guid.NewGuid(),
                            SeatNumber = "A1",
                            Status = SeatStatus.Available,
                            RowVersion = new byte[8]
                        }
                    }
                }
            };
            await dbContext.Bookings.AddAsync(booking);
            await dbContext.SaveChangesAsync();
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllBookings()
        {
            using var dbContext = new AppDbContext(_dbContextOptions);
            var repository = new BookingRepository(dbContext);

            await SeedDataAsync(dbContext);

            var result = await repository.GetAllAsync();

            Assert.NotEmpty(result);
            Assert.Single(result);
            Assert.Equal("A1", result.First().BookingSeats.First().Seat.SeatNumber);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnBooking_WhenBookingExists()
        {
            using var dbContext = new AppDbContext(_dbContextOptions);
            var repository = new BookingRepository(dbContext);

            await SeedDataAsync(dbContext);
            var existingBooking = dbContext.Bookings.Include(b => b.BookingSeats).First();

            var result = await repository.GetByIdAsync(existingBooking.Id);

            Assert.NotNull(result);
            Assert.Equal(existingBooking.Id, result.Id);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenBookingDoesNotExist()
        {
            using var dbContext = new AppDbContext(_dbContextOptions);
            var repository = new BookingRepository(dbContext);

            var result = await repository.GetByIdAsync(Guid.NewGuid());

            Assert.Null(result);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnUntrackedEntity_WhenNoTrackingIsTrue()
        {
            using var dbContext = new AppDbContext(_dbContextOptions);
            var repository = new BookingRepository(dbContext);

            await SeedDataAsync(dbContext);
            var existingBooking = dbContext.Bookings.Include(b => b.BookingSeats).First();

            var result = await repository.GetByIdAsync(existingBooking.Id, noTracking: true);

            Assert.NotNull(result);
            Assert.Equal(existingBooking.Id, result.Id);
            Assert.False(dbContext.Entry(result).IsKeySet && dbContext.Entry(result).State == EntityState.Unchanged); // Entity should not be tracked
        }

        [Fact]
        public async Task AddAsync_ShouldAddBooking()
        {
            using var dbContext = new AppDbContext(_dbContextOptions);
            var repository = new BookingRepository(dbContext);

            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                EventId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                BookingDate = DateTimeOffset.UtcNow,
                PaymentStatus = PaymentStatus.Pending,
                RowVersion = new byte[8]
            };

            await repository.AddAsync(booking);

            var addedBooking = await dbContext.Bookings.FindAsync(booking.Id);
            Assert.NotNull(addedBooking);
            Assert.Equal(booking.Id, addedBooking.Id);
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateBooking()
        {
            using var dbContext = new AppDbContext(_dbContextOptions);
            var repository = new BookingRepository(dbContext);

            await SeedDataAsync(dbContext);
            var booking = dbContext.Bookings.First();
            booking.PaymentStatus = PaymentStatus.Paid;

            await repository.UpdateAsync(booking);

            var updatedBooking = await dbContext.Bookings.FindAsync(booking.Id);
            Assert.Equal(PaymentStatus.Paid, updatedBooking.PaymentStatus);
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveBooking_WhenBookingExists()
        {
            using var dbContext = new AppDbContext(_dbContextOptions);
            var repository = new BookingRepository(dbContext);

            await SeedDataAsync(dbContext);
            var booking = dbContext.Bookings.First();

            await repository.DeleteAsync(booking.Id);

            var deletedBooking = await dbContext.Bookings.FindAsync(booking.Id);
            Assert.Null(deletedBooking);
        }

        [Fact]
        public async Task ReloadEntryAsync_ShouldReloadBooking()
        {
            using var dbContext = new AppDbContext(_dbContextOptions);
            var repository = new BookingRepository(dbContext);

            await SeedDataAsync(dbContext);
            var booking = dbContext.Bookings.First();
            booking.PaymentStatus = PaymentStatus.Paid;

            await repository.UpdateAsync(booking);

            booking.PaymentStatus = PaymentStatus.Pending;
            dbContext.Entry(booking).State = EntityState.Modified;
            await dbContext.SaveChangesAsync();

            await repository.ReloadEntryAsync(booking);

            Assert.Equal(PaymentStatus.Pending, booking.PaymentStatus);
        }
    }
}
