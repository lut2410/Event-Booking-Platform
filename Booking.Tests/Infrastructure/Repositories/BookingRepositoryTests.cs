using Bookings.Core.Entities;
using Bookings.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bookings.UnitTests.Infrastructure.Repositories
{
    public class BookingRepositoryTests
    {
        private readonly BookingRepository _bookingRepository;

        public BookingRepositoryTests()
        {
            _bookingRepository = new BookingRepository();
        }

        [Fact]
        public void AddBooking_ShouldStoreBooking()
        {
            // Arrange
            var booking = new Booking { UserId = 1, EventId = 1, BookingDate = System.DateTimeOffset.Now.AddDays(1) };

            // Act
            _bookingRepository.AddBooking(booking);

            // Assert
            var storedBooking = _bookingRepository.GetBookingById(booking.Id);
            Assert.NotNull(storedBooking);
            Assert.Equal(1, storedBooking.UserId);
        }

        [Fact]
        public void GetAllBookings_ShouldReturnEmptyListInitially()
        {
            // Act
            var bookings = _bookingRepository.GetAllBookings();

            // Assert
            Assert.Empty(bookings);
        }

        [Fact]
        public void GetBookingById_WithNonExistentId_ShouldReturnNull()
        {
            // Act
            var result = _bookingRepository.GetBookingById(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetAllBookings_ShouldReturnListOfStoredBookings()
        {
            // Arrange
            var booking1 = new Booking { UserId = 1, EventId = 1, BookingDate = System.DateTimeOffset.Now.AddDays(1) };
            var booking2 = new Booking { UserId = 2, EventId = 2, BookingDate = System.DateTimeOffset.Now.AddDays(2) };

            _bookingRepository.AddBooking(booking1);
            _bookingRepository.AddBooking(booking2);

            // Act
            var bookings = _bookingRepository.GetAllBookings().ToList();

            // Assert
            Assert.Equal(2, bookings.Count);
        }
    }
}
