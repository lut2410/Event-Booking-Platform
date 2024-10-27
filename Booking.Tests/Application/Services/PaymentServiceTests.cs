using System.Threading.Tasks;
using Bookings.Application.DTOs;
using Bookings.Application.Interfaces;
using Bookings.Application.Services;
using Moq;
using Stripe;
using Xunit;

namespace Bookings.Tests.Application.Services
{
    public class PaymentServiceTests
    {
        private readonly Mock<PaymentIntentService> _mockPaymentIntentService;
        private readonly Mock<RefundService> _mockRefundService;
        private readonly PaymentService _paymentService;

        public PaymentServiceTests()
        {
            _mockPaymentIntentService = new Mock<PaymentIntentService>();
            _mockRefundService = new Mock<RefundService>();
            _paymentService = new PaymentService(_mockPaymentIntentService.Object, _mockRefundService.Object);
        }

        [Fact]
        public async Task ProcessPaymentAsync_ShouldReturnSuccess_WhenPaymentIsSuccessful()
        {
            // Arrange
            var paymentRequest = new PaymentRequest
            {
                Amount = 5000,
                PaymentMethodId = "pm_card_visa"
            };

            var paymentIntent = new PaymentIntent
            {
                Id = "pi_123456",
                Status = "succeeded"
            };

            _mockPaymentIntentService
                .Setup(service => service.CreateAsync(It.IsAny<PaymentIntentCreateOptions>(), null, default))
                .ReturnsAsync(paymentIntent);

            // Act
            var result = await _paymentService.ProcessPaymentAsync(paymentRequest);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("succeeded", result.Message);
            Assert.Equal("pi_123456", result.PaymentIntentId);
        }

        [Fact]
        public async Task ProcessPaymentAsync_ShouldReturnFailure_WhenPaymentFails()
        {
            // Arrange
            var paymentRequest = new PaymentRequest
            {
                Amount = 5000,
                PaymentMethodId = "pm_card_visa"
            };

            var stripeException = new StripeException
            {
                StripeError = new StripeError { Message = "Payment failed due to an error." }
            };

            _mockPaymentIntentService
                .Setup(service => service.CreateAsync(It.IsAny<PaymentIntentCreateOptions>(), null, default))
                .ThrowsAsync(stripeException);

            // Act
            var result = await _paymentService.ProcessPaymentAsync(paymentRequest);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Payment failed due to an error.", result.Message);
            Assert.Null(result.PaymentIntentId);
        }

        [Fact]
        public async Task ProcessRefundAsync_ShouldReturnSuccess_WhenRefundIsSuccessful()
        {
            // Arrange
            var refundRequest = new RefundRequest
            {
                PaymentIntentId = "pi_123456",
                Amount = 5000,
                Reason = "requested_by_customer"
            };

            var refund = new Refund
            {
                Id = "re_123456",
                Status = "succeeded"
            };

            _mockRefundService
                .Setup(service => service.CreateAsync(It.IsAny<RefundCreateOptions>(), null, default))
                .ReturnsAsync(refund);

            // Act
            var result = await _paymentService.ProcessRefundAsync(refundRequest);

            // Assert
            Assert.Equal("succeeded", result);
        }

        [Fact]
        public async Task ProcessRefundAsync_ShouldReturnFailure_WhenRefundFails()
        {
            // Arrange
            var refundRequest = new RefundRequest
            {
                PaymentIntentId = "pi_123456",
                Amount = 5000,
                Reason = "requested_by_customer"
            };

            var stripeException = new StripeException
            {
                StripeError = new StripeError { Message = "Refund failed due to an error." }
            };

            _mockRefundService
                .Setup(service => service.CreateAsync(It.IsAny<RefundCreateOptions>(), null, default))
                .ThrowsAsync(stripeException);

            // Act
            var result = await _paymentService.ProcessRefundAsync(refundRequest);

            // Assert
            Assert.Equal("Refund failed due to an error.", result);
        }
    }
}
