using Bookings.Application.DTOs;
using Bookings.Application.Services;
using Moq;
using Stripe;
using System.Threading.Tasks;
using Xunit;

namespace Bookings.Tests.Application.Services
{
    public class PaymentServiceTests
    {
        private readonly Mock<PaymentIntentService> _mockPaymentIntentService;

        public PaymentServiceTests()
        {
            _mockPaymentIntentService = new Mock<PaymentIntentService>();
        }

        [Fact]
        public async Task ProcessPaymentAsync_ShouldReturnSucceeded_WhenPaymentIsSuccessful()
        {
            // Arrange
            var paymentRequest = new PaymentRequest
            {
                Amount = 5000,
                PaymentMethodId = "pm_card_visa"
            };

            _mockPaymentIntentService
                .Setup(service => service.CreateAsync(It.IsAny<PaymentIntentCreateOptions>(), null, default))
                .ReturnsAsync(new PaymentIntent { Status = "succeeded" });

            var paymentService = new PaymentService(_mockPaymentIntentService.Object);

            // Act
            var result = await paymentService.ProcessPaymentAsync(paymentRequest);

            // Assert
            Assert.Equal("succeeded", result);
        }

        [Fact]
        public async Task ProcessPaymentAsync_ShouldReturnErrorMessage_WhenPaymentFails()
        {
            // Arrange
            var paymentRequest = new PaymentRequest
            {
                Amount = 5000,
                PaymentMethodId = "pm_card_visa"
            };

            _mockPaymentIntentService
                .Setup(service => service.CreateAsync(It.IsAny<PaymentIntentCreateOptions>(), null, default))
                .ThrowsAsync(new StripeException
                {
                    StripeError = new StripeError { Message = "Your card was declined." }
                });

            var paymentService = new PaymentService(_mockPaymentIntentService.Object);

            // Act
            var result = await paymentService.ProcessPaymentAsync(paymentRequest);

            // Assert
            Assert.Equal("Your card was declined.", result);
        }

        [Fact]
        public async Task ProcessPaymentAsync_ShouldReturnRequiresAction_WhenFurtherActionIsRequired()
        {
            // Arrange
            var paymentRequest = new PaymentRequest
            {
                Amount = 5000,
                PaymentMethodId = "pm_card_visa"
            };

            _mockPaymentIntentService
                .Setup(service => service.CreateAsync(It.IsAny<PaymentIntentCreateOptions>(), null, default))
                .ReturnsAsync(new PaymentIntent { Status = "requires_action" });

            var paymentService = new PaymentService(_mockPaymentIntentService.Object);

            // Act
            var result = await paymentService.ProcessPaymentAsync(paymentRequest);

            // Assert
            Assert.Equal("requires_action", result);
        }
    }
}
