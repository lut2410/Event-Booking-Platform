using Bookings.Application.DTOs;

namespace Bookings.Application.Interfaces
{
    public interface IPaymentService
    {
        Task<PaymentResult> ProcessPaymentAsync(PaymentRequest paymentRequest);
        Task<string> ProcessRefundAsync(RefundRequest refundRequest);
    }
}
