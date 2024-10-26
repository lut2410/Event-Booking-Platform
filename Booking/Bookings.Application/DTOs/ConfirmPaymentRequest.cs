namespace Bookings.Application.DTOs
{
    public class ConfirmPaymentRequest
    {
        public Guid BookingId { get; set; }
        public Guid UserId { get; set; }
        public PaymentRequest PaymentRequest { get; set; }
    }
}
