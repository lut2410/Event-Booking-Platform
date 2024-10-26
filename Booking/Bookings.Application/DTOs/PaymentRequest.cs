namespace Bookings.Application.DTOs
{
    public class PaymentRequest
    {
        public long Amount { get; set; }
        public string PaymentMethodId { get; set; }
    }
}
