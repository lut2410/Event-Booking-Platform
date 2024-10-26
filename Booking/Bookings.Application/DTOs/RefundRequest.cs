using System;

namespace Bookings.Application.DTOs
{
    public class RefundRequest
    {
        public string PaymentIntentId { get; set; }
        public long? Amount { get; set; }
        public string Reason { get; set; }
    }
}
