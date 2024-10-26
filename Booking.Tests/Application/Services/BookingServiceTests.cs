using Bookings.Application.DTOs;
using Bookings.Application.Interfaces;
using Bookings.Application.Services;
using Bookings.Core.Entities;
using Bookings.Core.Interfaces.Repositories;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Bookings.Tests.Services
{
    public class BookingServiceTests
    {
        private readonly Mock<IBookingRepository> _mockBookingRepository;
        private readonly Mock<ISeatRepository> _mockSeatRepository;
        private readonly Mock<IPaymentService> _mockPaymentService;
        private readonly BookingService _bookingService;

        public BookingServiceTests()
        {
            _mockBookingRepository = new Mock<IBookingRepository>();
            _mockSeatRepository = new Mock<ISeatRepository>();
            _mockPaymentService = new Mock<IPaymentService>();
            _bookingService = new BookingService(_mockBookingRepository.Object, _mockSeatRepository.Object, _mockPaymentService.Object);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllBookings()
        {
            // Arrange
            var bookings = new List<Booking>
            {
                new Booking
                {
                    Id = Guid.NewGuid(),
                    BookingDate = DateTimeOffset.UtcNow,
                    PaymentStatus = PaymentStatus.Paid,
                    EventId = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    BookingSeats = new List<BookingSeat>
                    {
                        new BookingSeat
                        {
                            Seat = new Seat { Id = Guid.NewGuid(), SeatNumber = "A1", Status = SeatStatus.Booked }
                        }
                    }
                }
            };

            _mockBookingRepository.Setup(repo => repo.GetAllAsync()).ReturnsAsync(bookings);

            // Act
            var result = await _bookingService.GetAllAsync();

            // Assert
            Assert.Single(result);
            Assert.Equal(bookings[0].Id, result.First().Id);
            Assert.Equal("A1", result.First().Seats.First().SeatNumber);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnBookingById()
        {
            // Arrange
            var bookingId = Guid.NewGuid();
            var booking = new Booking
            {
                Id = bookingId,
                BookingDate = DateTimeOffset.UtcNow,
                PaymentStatus = PaymentStatus.Paid,
                EventId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                BookingSeats = new List<BookingSeat>
                {
                    new BookingSeat
                    {
                        Seat = new Seat { Id = Guid.NewGuid(), SeatNumber = "B1", Status = SeatStatus.Booked }
                    }
                }
            };

            _mockBookingRepository.Setup(repo => repo.GetByIdAsync(bookingId)).ReturnsAsync(booking);

            // Act
            var result = await _bookingService.GetByIdAsync(bookingId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(bookingId, result.Id);
            Assert.Equal("B1", result.Seats.First().SeatNumber);
        }

        [Fact]
        public async Task ReserveSeatsAsync_ShouldReserveSeatsSuccessfully()
        {
            // Arrange
            var seatIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            var eventId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var seats = seatIds.Select(id => new Seat { Id = id, EventId = eventId, Status = SeatStatus.Available }).ToList();

            _mockSeatRepository.Setup(repo => repo.GetSeatsByIdsAsync(seatIds)).ReturnsAsync(seats);

            // Act
            var result = await _bookingService.ReserveSeatsAsync(eventId, userId, seatIds);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Seats reserved successfully.", result.Message);
            _mockBookingRepository.Verify(repo => repo.AddAsync(It.IsAny<Booking>()), Times.Once);
            _mockSeatRepository.Verify(repo => repo.UpdateSeatsAsync(It.IsAny<List<Seat>>()), Times.Once);
        }

        [Fact]
        public async Task ReserveSeatsAsync_ShouldFail_WhenSeatsAreNotAvailable()
        {
            // Arrange
            var seatIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            var eventId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var seats = seatIds.Select(id => new Seat { Id = id, EventId = eventId, Status = SeatStatus.Booked }).ToList(); // Set seats as booked

            _mockSeatRepository.Setup(repo => repo.GetSeatsByIdsAsync(seatIds)).ReturnsAsync(seats);

            // Act
            var result = await _bookingService.ReserveSeatsAsync(eventId, userId, seatIds);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("One or more seats are no longer available.", result.Message);
            _mockBookingRepository.Verify(repo => repo.AddAsync(It.IsAny<Booking>()), Times.Never);
        }

        [Fact]
        public async Task ConfirmPaymentAsync_ShouldConfirmPayment_WhenPaymentSucceeds()
        {
            // Arrange
            var bookingId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var booking = new Booking
            {
                Id = bookingId,
                UserId = userId,
                PaymentStatus = PaymentStatus.Pending,
                BookingSeats = new List<BookingSeat>
                {
                    new BookingSeat
                    {
                        Seat = new Seat { Id = Guid.NewGuid(), Status = SeatStatus.Reserved }
                    }
                }
            };

            var paymentRequest = new PaymentRequest { Amount = 5000, PaymentMethodId = "pm_card_visa" };

            _mockBookingRepository.Setup(repo => repo.GetByIdAsync(bookingId)).ReturnsAsync(booking);
            _mockPaymentService.Setup(service => service.ProcessPaymentAsync(paymentRequest)).ReturnsAsync("succeeded");

            // Act
            var result = await _bookingService.ConfirmPaymentAsync(bookingId, userId, paymentRequest);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Payment successful. Seats confirmed.", result.Message);
            _mockBookingRepository.Verify(repo => repo.UpdateAsync(It.Is<Booking>(b => b.PaymentStatus == PaymentStatus.Paid)), Times.Once);
        }

        [Fact]
        public async Task ConfirmPaymentAsync_ShouldFail_WhenPaymentFails()
        {
            // Arrange
            var bookingId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var booking = new Booking
            {
                Id = bookingId,
                UserId = userId,
                PaymentStatus = PaymentStatus.Pending,
                BookingSeats = new List<BookingSeat>
                {
                    new BookingSeat
                    {
                        Seat = new Seat { Id = Guid.NewGuid(), Status = SeatStatus.Reserved }
                    }
                }
            };

            var paymentRequest = new PaymentRequest { Amount = 5000, PaymentMethodId = "pm_card_visa" };

            _mockBookingRepository.Setup(repo => repo.GetByIdAsync(bookingId)).ReturnsAsync(booking);
            _mockPaymentService.Setup(service => service.ProcessPaymentAsync(paymentRequest)).ReturnsAsync("card_declined");

            // Act
            var result = await _bookingService.ConfirmPaymentAsync(bookingId, userId, paymentRequest);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("card_declined", result.Message);
            _mockBookingRepository.Verify(repo => repo.UpdateAsync(It.IsAny<Booking>()), Times.Never);
        }
    }
}
