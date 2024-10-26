using Bookings.Application.DTOs;

namespace Bookings.Application.Interfaces
{
    public interface IPaymentService
    {
        Task<string> ProcessPaymentAsync(PaymentRequest paymentRequest);
    }
}
