using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bookings.Application.DTOs;
using Bookings.Application.Interfaces;
using Bookings.Application.Services;
using Bookings.Core.Entities;
using Bookings.Core.Interfaces.Repositories;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Bookings.Tests.Application.Services
{
    public class BookingServiceTests
    {
        private readonly Mock<IBookingRepository> _mockBookingRepository;
        private readonly Mock<ISeatRepository> _mockSeatRepository;
        private readonly Mock<IPaymentService> _mockPaymentService;
        private readonly Mock<IRedisSeatReservationService> _mockRedisService;
        private readonly IConfiguration _configuration;
        private readonly BookingService _bookingService;

        public BookingServiceTests()
        {
            _mockBookingRepository = new Mock<IBookingRepository>();
            _mockSeatRepository = new Mock<ISeatRepository>();
            _mockPaymentService = new Mock<IPaymentService>();
            _mockRedisService = new Mock<IRedisSeatReservationService>();

            var inMemorySettings = new Dictionary<string, string>
            {
                { "SeatReservation:TimeInMinutes", "10" }
            };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            // Initialize BookingService with mocks
            _bookingService = new BookingService(
                _mockBookingRepository.Object,
                _mockSeatRepository.Object,
                _mockPaymentService.Object,
                _mockRedisService.Object,
                _configuration);
        }

        [Fact]
        public async Task ReserveSeatsAsync_ShouldReturnSuccess_WhenSeatsReservedSuccessfully()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var seatIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            var seats = seatIds.Select(id => new Seat
            {
                Id = id,
                EventId = eventId,
                Status = SeatStatus.Available
            }).ToList();

            _mockRedisService
                .Setup(service => service.TryReserveSeatsAsync(eventId, userId, seatIds, It.IsAny<TimeSpan>()))
                .ReturnsAsync(true);

            _mockSeatRepository
                .Setup(repo => repo.GetSeatsByIdsAsync(seatIds, true))
                .ReturnsAsync(seats);

            _mockBookingRepository
                .Setup(repo => repo.AddAsync(It.IsAny<Booking>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _bookingService.ReserveSeatsAsync(eventId, userId, seatIds);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Seats reserved successfully.", result.Message);
            _mockRedisService.Verify(service => service.TryReserveSeatsAsync(eventId, userId, seatIds, It.IsAny<TimeSpan>()), Times.Once);
            _mockBookingRepository.Verify(repo => repo.AddAsync(It.IsAny<Booking>()), Times.Once);
        }

        [Fact]
        public async Task ReserveSeatsAsync_ShouldReturnFailure_WhenRedisReservationFails()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var seatIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

            _mockRedisService
                .Setup(service => service.TryReserveSeatsAsync(eventId, userId, seatIds, It.IsAny<TimeSpan>()))
                .ReturnsAsync(false);

            // Act
            var result = await _bookingService.ReserveSeatsAsync(eventId, userId, seatIds);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Some seats are no longer available.", result.Message);
            _mockRedisService.Verify(service => service.TryReserveSeatsAsync(eventId, userId, seatIds, It.IsAny<TimeSpan>()), Times.Once);
            _mockBookingRepository.Verify(repo => repo.AddAsync(It.IsAny<Booking>()), Times.Never);
        }

        [Fact]
        public async Task ReserveSeatsAsync_ShouldReleaseRedisLock_WhenDatabaseReservationFails()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var seatIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            var seats = seatIds.Select(id => new Seat
            {
                Id = id,
                EventId = eventId,
                Status = SeatStatus.Booked // Simulate unavailable seat
            }).ToList();

            _mockRedisService
                .Setup(service => service.TryReserveSeatsAsync(eventId, userId, seatIds, It.IsAny<TimeSpan>()))
                .ReturnsAsync(true);

            _mockSeatRepository
                .Setup(repo => repo.GetSeatsByIdsAsync(seatIds, true))
                .ReturnsAsync(seats);

            // Act
            var result = await _bookingService.ReserveSeatsAsync(eventId, userId, seatIds);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("One or more seats are no longer available.", result.Message);
            _mockRedisService.Verify(service => service.ReleaseSeatsAsync(eventId, userId, seatIds), Times.Once);
            _mockBookingRepository.Verify(repo => repo.AddAsync(It.IsAny<Booking>()), Times.Never);
        }
    }
}
