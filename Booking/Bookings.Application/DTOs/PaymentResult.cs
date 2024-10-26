using Bookings.Core.Entities;

namespace Bookings.Application.DTOs
{
    public class PaymentResult
    {
        public bool Success { get; set; }
        public string PaymentIntentId { get; set; }
        public string Message { get; set; }
    }
}
