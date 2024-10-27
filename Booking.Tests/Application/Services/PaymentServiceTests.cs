using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Stripe;
using Bookings.Application.DTOs;
using Bookings.Application.Interfaces;
using Bookings.Application.Services;

namespace Bookings.Application.Tests.Services
{
    public class PaymentServiceTests
    {
        private readonly Mock<PaymentIntentService> _paymentIntentServiceMock;
        private readonly Mock<RefundService> _refundServiceMock;
        private readonly Mock<ILogger<PaymentService>> _loggerMock;
        private readonly PaymentService _paymentService;

        public PaymentServiceTests()
        {
            _paymentIntentServiceMock = new Mock<PaymentIntentService>();
            _refundServiceMock = new Mock<RefundService>();
            _loggerMock = new Mock<ILogger<PaymentService>>();

            _paymentService = new PaymentService(
                _paymentIntentServiceMock.Object,
                _refundServiceMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task ProcessPaymentAsync_ShouldReturnSuccess_WhenPaymentSucceeds()
        {
            // Arrange
            var paymentRequest = new PaymentRequest
            {
                Amount = 1000,
                PaymentMethodId = "pm_test"
            };

            var paymentIntent = new PaymentIntent
            {
                Id = "pi_test",
                Status = "succeeded"
            };

            _paymentIntentServiceMock
                .Setup(x => x.CreateAsync(It.IsAny<PaymentIntentCreateOptions>(), null, default))
                .ReturnsAsync(paymentIntent);

            // Act
            var result = await _paymentService.ProcessPaymentAsync(paymentRequest);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("pi_test", result.PaymentIntentId);
            Assert.Equal("succeeded", result.Message);
            _loggerMock.Verify(log => log.LogInformation("Payment processing completed with Status: {Status} for PaymentIntentId: {PaymentIntentId}", "succeeded", "pi_test"), Times.Once);
        }

        [Fact]
        public async Task ProcessPaymentAsync_ShouldReturnFailure_WhenPaymentFailsDueToException()
        {
            // Arrange
            var paymentRequest = new PaymentRequest
            {
                Amount = 1000,
                PaymentMethodId = "pm_test"
            };

            _paymentIntentServiceMock
                .Setup(x => x.CreateAsync(It.IsAny<PaymentIntentCreateOptions>(), null, default))
                .ThrowsAsync(new StripeException
                {
                    StripeError = new StripeError
                    {
                        Message = "Payment failed due to insufficient funds."
                    }
                });

            // Act
            var result = await _paymentService.ProcessPaymentAsync(paymentRequest);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Payment failed due to insufficient funds.", result.Message);
            _loggerMock.Verify(log => log.LogError(It.IsAny<StripeException>(), "Payment failed for PaymentMethodId: {PaymentMethodId} with error: {Error}", paymentRequest.PaymentMethodId, "Payment failed due to insufficient funds."), Times.Once);
        }

        [Fact]
        public async Task ProcessRefundAsync_ShouldReturnSuccess_WhenRefundSucceeds()
        {
            // Arrange
            var refundRequest = new RefundRequest
            {
                PaymentIntentId = "pi_test",
                Amount = 500,
                Reason = "requested_by_customer"
            };

            var refund = new Refund
            {
                Id = "re_test",
                Status = "succeeded"
            };

            _refundServiceMock
                .Setup(x => x.CreateAsync(It.IsAny<RefundCreateOptions>(), null, default))
                .ReturnsAsync(refund);

            // Act
            var result = await _paymentService.ProcessRefundAsync(refundRequest);

            // Assert
            Assert.Equal("succeeded", result);
            _loggerMock.Verify(log => log.LogInformation("Refund processing completed with Status: {Status} for RefundId: {RefundId}", "succeeded", "re_test"), Times.Once);
        }

        [Fact]
        public async Task ProcessRefundAsync_ShouldReturnFailure_WhenRefundFailsDueToException()
        {
            // Arrange
            var refundRequest = new RefundRequest
            {
                PaymentIntentId = "pi_test",
                Amount = 500,
                Reason = "requested_by_customer"
            };

            _refundServiceMock
                .Setup(x => x.CreateAsync(It.IsAny<RefundCreateOptions>(), null, default))
                .ThrowsAsync(new StripeException
                {
                    StripeError = new StripeError
                    {
                        Message = "Refund failed due to network error."
                    }
                });

            // Act
            var result = await _paymentService.ProcessRefundAsync(refundRequest);

            // Assert
            Assert.Equal("Refund failed due to network error.", result);
            _loggerMock.Verify(log => log.LogError(It.IsAny<StripeException>(), "Refund failed for PaymentIntentId: {PaymentIntentId} with error: {Error}", refundRequest.PaymentIntentId, "Refund failed due to network error."), Times.Once);
        }
    }
}
