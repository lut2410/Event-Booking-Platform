// File: Bookings.Application/Services/PaymentService.cs

using Bookings.Application.DTOs;
using Bookings.Application.Interfaces;
using Stripe;
using System.Threading.Tasks;

namespace Bookings.Application.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly PaymentIntentService _paymentIntentService;

        public PaymentService(PaymentIntentService paymentIntentService)
        {
            _paymentIntentService = paymentIntentService;
        }

        public async Task<string> ProcessPaymentAsync(PaymentRequest paymentRequest)
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

                return paymentIntent.Status;
            }
            catch (StripeException ex)
            {
                return ex.StripeError?.Message ?? "Payment failed due to an error.";
            }
        }
    }
}
