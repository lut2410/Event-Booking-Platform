using Bookings.Application.DTOs;
using Bookings.Application.Interfaces;
using Bookings.Core.Entities;
using Stripe;
using System.Threading.Tasks;

namespace Bookings.Application.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly PaymentIntentService _paymentIntentService;
        private readonly RefundService _refundService;

        public PaymentService(PaymentIntentService paymentIntentService, RefundService refundService)
        {
            _paymentIntentService = paymentIntentService;
            _refundService = refundService;
        }

        public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest paymentRequest)
        {
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

                return new PaymentResult
                {
                    PaymentIntentId = paymentIntent.Id,
                    Success = paymentIntent.Status == "succeeded",
                    Message = paymentIntent.Status
                };
            }
            catch (StripeException ex)
            {
                return new PaymentResult
                {
                    Success = false,
                    Message = ex.StripeError?.Message ?? "Payment failed due to an error."
                };
            }
        }

        public async Task<string> ProcessRefundAsync(RefundRequest refundRequest)
        {
            try
            {
                var refundOptions = new RefundCreateOptions
                {
                    PaymentIntent = refundRequest.PaymentIntentId,
                    Amount = refundRequest.Amount,
                    Reason = refundRequest.Reason
                };

                var refund = await _refundService.CreateAsync(refundOptions);

                return refund.Status;
            }
            catch (StripeException ex)
            {
                return ex.StripeError?.Message ?? "Refund failed due to an error.";
            }
        }
    }
}
