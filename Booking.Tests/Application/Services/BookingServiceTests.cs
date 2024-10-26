using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bookings.Application.DTOs;
using Bookings.Application.Interfaces;
using Bookings.Application.Services;
using Bookings.Core.Entities;
using Bookings.Core.Interfaces.Repositories;
using Moq;
using Stripe.Billing;
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
                        new BookingSeat { Seat = new Seat { Id = Guid.NewGuid(), SeatNumber = "A1", Status = SeatStatus.Booked } }
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
                    new BookingSeat { Seat = new Seat { Id = Guid.NewGuid(), SeatNumber = "B1", Status = SeatStatus.Booked } }
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
                    new BookingSeat { Seat = new Seat { Id = Guid.NewGuid(), Status = SeatStatus.Reserved } }
                }
            };

            var paymentRequest = new PaymentRequest { Amount = 5000, PaymentMethodId = "pm_card_visa" };
            _mockBookingRepository.Setup(repo => repo.GetByIdAsync(bookingId)).ReturnsAsync(booking);
            _mockPaymentService.Setup(service => service.ProcessPaymentAsync(paymentRequest))
                .ReturnsAsync(new PaymentResult { Success = true, PaymentIntentId = "pi_123456" });

            // Act
            var result = await _bookingService.ConfirmPaymentAsync(bookingId, userId, paymentRequest);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Payment successful. Seats confirmed.", result.Message);
            Assert.Equal(PaymentStatus.Paid, booking.PaymentStatus);
            Assert.Equal("pi_123456", booking.PaymentIntentId);
            _mockBookingRepository.Verify(repo => repo.UpdateAsync(booking), Times.Once);
        }

        [Fact]
        public async Task RequestRefundAsync_ShouldReturnSuccess_WhenRefundIsProcessed()
        {
            // Arrange
            var bookingId = Guid.NewGuid();
            var booking = new Booking
            {
                Id = bookingId,
                PaymentStatus = PaymentStatus.Paid,
                PaymentIntentId = "pi_123456"
            };

            _mockBookingRepository.Setup(repo => repo.GetByIdAsync(bookingId)).ReturnsAsync(booking);
            _mockPaymentService.Setup(service => service.ProcessRefundAsync(It.IsAny<RefundRequest>()))
                .ReturnsAsync("succeeded");

            // Act
            var result = await _bookingService.RequestRefundAsync(bookingId, new RefundRequest { PaymentIntentId = "pi_123456" });

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Refund successful.", result.Message);
            Assert.Equal(PaymentStatus.Refunded, booking.PaymentStatus);
            _mockBookingRepository.Verify(repo => repo.UpdateAsync(booking), Times.Once);
        }

        [Fact]
        public async Task SelfRequestRefundAsync_ShouldReturnSuccess_WhenSelfRefundIsEligible()
        {
            // Arrange
            var bookingId = Guid.NewGuid();
            var booking = new Booking
            {
                Id = bookingId,
                PaymentStatus = PaymentStatus.Paid,
                PaymentIntentId = "pi_123456",
                BookingDate = DateTimeOffset.UtcNow.AddDays(-5)
            };

            _mockBookingRepository.Setup(repo => repo.GetByIdAsync(bookingId)).ReturnsAsync(booking);
            _mockPaymentService.Setup(service => service.ProcessRefundAsync(It.IsAny<RefundRequest>()))
                .ReturnsAsync("succeeded");

            // Act
            var result = await _bookingService.SelfRequestRefundAsync(bookingId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Refund successful.", result.Message);
            Assert.Equal(PaymentStatus.Refunded, booking.PaymentStatus);
            _mockBookingRepository.Verify(repo => repo.UpdateAsync(booking), Times.Once);
        }
    }
}
