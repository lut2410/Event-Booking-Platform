using Bookings.Core.Entities;

namespace Bookings.Application.DTOs
{
    public class ReservationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public Guid CreatedBookingId { get; set; }
        public DateTimeOffset? ReservationExpiresAt { get; set; }
    }
}
