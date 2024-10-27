using Bookings.Application.DTOs;
using Bookings.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Stripe;
using System.Threading.Tasks;

namespace Bookings.Application.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly PaymentIntentService _paymentIntentService;
        private readonly RefundService _refundService;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(PaymentIntentService paymentIntentService, RefundService refundService, ILogger<PaymentService> logger)
        {
            _paymentIntentService = paymentIntentService;
            _refundService = refundService;
            _logger = logger;
        }

        public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest paymentRequest)
        {
            _logger.LogInformation("Initiating payment for PaymentMethodId: {PaymentMethodId}, Amount: {Amount} cents",
                                   paymentRequest.PaymentMethodId, paymentRequest.Amount);

            try
            {
                var paymentIntent = await _paymentIntentService.CreateAsync(new PaymentIntentCreateOptions
                {
                    Amount = paymentRequest.Amount,
                    Currency = "usd",
                    PaymentMethod = paymentRequest.PaymentMethodId,
                    Confirm = true,
                    AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                    {
                        Enabled = true,
                        AllowRedirects = "never"
                    }
                });

                bool paymentSucceeded = paymentIntent.Status == "succeeded";
                _logger.LogInformation("Payment processing completed with Status: {Status} for PaymentIntentId: {PaymentIntentId}",
                                       paymentIntent.Status, paymentIntent.Id);

                return new PaymentResult
                {
                    PaymentIntentId = paymentIntent.Id,
                    Success = paymentSucceeded,
                    Message = paymentIntent.Status
                };
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Payment failed for PaymentMethodId: {PaymentMethodId} with error: {Error}",
                                 paymentRequest.PaymentMethodId, ex.StripeError?.Message);

                return new PaymentResult
                {
                    Success = false,
                    Message = ex.StripeError?.Message ?? "Payment failed due to an error."
                };
            }
        }

        public async Task<string> ProcessRefundAsync(RefundRequest refundRequest)
        {
            _logger.LogInformation("Initiating refund for PaymentIntentId: {PaymentIntentId}, Amount: {Amount} cents",
                                   refundRequest.PaymentIntentId, refundRequest.Amount);

            try
            {
                var refundOptions = new RefundCreateOptions
                {
                    PaymentIntent = refundRequest.PaymentIntentId,
                    Amount = refundRequest.Amount,
                    Reason = refundRequest.Reason
                };

                var refund = await _refundService.CreateAsync(refundOptions);

                _logger.LogInformation("Refund processing completed with Status: {Status} for RefundId: {RefundId}",
                                       refund.Status, refund.Id);

                return refund.Status;
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Refund failed for PaymentIntentId: {PaymentIntentId} with error: {Error}",
                                 refundRequest.PaymentIntentId, ex.StripeError?.Message);

                return ex.StripeError?.Message ?? "Refund failed due to an error.";
            }
        }
    }
}
