using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Bookings.Application.DTOs;
using Bookings.Application.Interfaces;
using Bookings.Application.Services;
using Bookings.Core.Entities;
using Bookings.Core.Interfaces.Repositories;

namespace Bookings.Application.Tests.Services
{
    public class BookingServiceTests
    {
        private readonly Mock<IBookingRepository> _bookingRepositoryMock;
        private readonly Mock<ISeatRepository> _seatRepositoryMock;
        private readonly Mock<IPaymentService> _paymentServiceMock;
        private readonly Mock<IRedisSeatReservationService> _reservationCacheServiceMock;
        private readonly Mock<IRedisFraudDetectionService> _fraudDetectionServiceMock;
        private readonly Mock<ILogger<BookingService>> _loggerMock;
        private readonly BookingService _bookingService;
        private readonly IConfiguration _configuration;

        public BookingServiceTests()
        {
            _bookingRepositoryMock = new Mock<IBookingRepository>();
            _seatRepositoryMock = new Mock<ISeatRepository>();
            _paymentServiceMock = new Mock<IPaymentService>();
            _reservationCacheServiceMock = new Mock<IRedisSeatReservationService>();
            _fraudDetectionServiceMock = new Mock<IRedisFraudDetectionService>();
            _loggerMock = new Mock<ILogger<BookingService>>();

            var inMemorySettings = new Dictionary<string, string>
            {
                {"SeatReservation:TimeInMinutes", "10"}
            };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            _bookingService = new BookingService(
                _bookingRepositoryMock.Object,
                _seatRepositoryMock.Object,
                _paymentServiceMock.Object,
                _reservationCacheServiceMock.Object,
                _fraudDetectionServiceMock.Object,
                _configuration,
                _loggerMock.Object);
        }

        [Fact]
        public async Task ReserveSeatsAsync_ShouldReturnSuccess_WhenSeatsAvailable()
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

            _reservationCacheServiceMock.Setup(r => r.TryReserveSeatsAsync(eventId, userId, seatIds, It.IsAny<TimeSpan>())).ReturnsAsync(true);
            _seatRepositoryMock.Setup(s => s.GetSeatsByIdsAsync(seatIds, true)).ReturnsAsync(seats);
            _bookingRepositoryMock.Setup(b => b.AddAsync(It.IsAny<Booking>())).Returns(Task.CompletedTask);

            // Act
            var result = await _bookingService.ReserveSeatsAsync(eventId, userId, seatIds);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Seats reserved successfully.", result.Message);
            Assert.NotNull(result.CreatedBookingId);

            _seatRepositoryMock.Verify(s => s.UpdateSeatsAsync(It.Is<List<Seat>>(seats => seats.All(seat => seat.Status == SeatStatus.Reserved))), Times.Once);
            _loggerMock.Verify(
                log => log.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Seats successfully reserved for BookingId")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ReserveSeatsAsync_ShouldReturnFailure_WhenUserBlockedForFraud()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var seatIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

            _fraudDetectionServiceMock.Setup(x => x.IsUserBlockedAsync(userId)).ReturnsAsync(true);

            // Act
            var result = await _bookingService.ReserveSeatsAsync(eventId, userId, seatIds);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Reservation blocked due to suspected fraud.", result.Message);
        }

        [Fact]
        public async Task ConfirmPaymentAsync_ShouldReturnSuccess_WhenPaymentSuccessful()
        {
            // Arrange
            var bookingId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var paymentRequest = new PaymentRequest { Amount = 100, PaymentMethodId = "pm_test" };

            _bookingRepositoryMock.Setup(x => x.GetByIdAsync(bookingId, true)).ReturnsAsync(new Booking
            {
                Id = bookingId,
                UserId = userId,
                PaymentStatus = PaymentStatus.Pending,
                BookingSeats = new List<BookingSeat>
                {
                    new BookingSeat { Seat = new Seat { Status = SeatStatus.Reserved } }
                }
            });

            _paymentServiceMock.Setup(x => x.ProcessPaymentAsync(paymentRequest)).ReturnsAsync(new PaymentResult
            {
                Success = true,
                PaymentIntentId = "pi_test"
            });

            // Act
            var result = await _bookingService.ConfirmPaymentAsync(bookingId, userId, paymentRequest);

            // Assert
            Assert.True(result.Success);
            _loggerMock.Verify(
           x => x.Log(
               LogLevel.Information,
               It.IsAny<EventId>(),
               It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Payment confirmed for BookingId")),
               It.IsAny<Exception>(),
               It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
           Times.Once);
        }
        [Fact]
        public async Task RequestRefundAsync_ShouldReturnSuccess_WhenRefundSuccessful()
        {
            // Arrange
            var bookingId = Guid.NewGuid();
            var refundRequest = new RefundRequest { PaymentIntentId = "pi_test", Amount = 1000, Reason = "requested_by_customer" };

            var booking = new Booking
            {
                Id = bookingId,
                PaymentStatus = PaymentStatus.Paid,
                PaymentIntentId = refundRequest.PaymentIntentId
            };

            _bookingRepositoryMock.Setup(b => b.GetByIdAsync(bookingId, true)).ReturnsAsync(booking);
            _paymentServiceMock.Setup(p => p.ProcessRefundAsync(refundRequest)).ReturnsAsync("succeeded");

            // Act
            var result = await _bookingService.RequestRefundAsync(bookingId, refundRequest);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Refund successful.", result.Message);

            _bookingRepositoryMock.Verify(b => b.UpdateAsync(It.Is<Booking>(b => b.PaymentStatus == PaymentStatus.Refunded)), Times.Once);
            _loggerMock.Verify(
                log => log.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Processing refund for BookingId")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);

            _loggerMock.Verify(
                log => log.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Refund successful for BookingId")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
        [Fact]
        public async Task SelfRequestRefundAsync_ShouldReturnFailure_WhenBookingNotEligibleForRefund()
        {
            // Arrange
            var bookingId = Guid.NewGuid();
            var booking = new Booking
            {
                Id = bookingId,
                PaymentStatus = PaymentStatus.Pending, // Not eligible status for refund
                BookingDate = DateTimeOffset.UtcNow
            };

            _bookingRepositoryMock.Setup(repo => repo.GetByIdAsync(bookingId, true)).ReturnsAsync(booking);

            // Act
            var result = await _bookingService.SelfRequestRefundAsync(bookingId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Booking is not eligible for a refund.", result.Message);

            _paymentServiceMock.Verify(service => service.ProcessRefundAsync(It.IsAny<RefundRequest>()), Times.Never);

            _loggerMock.Verify(
                log => log.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Refund request denied")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
}
