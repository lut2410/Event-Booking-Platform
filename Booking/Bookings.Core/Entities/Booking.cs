using System.ComponentModel.DataAnnotations;

namespace Bookings.Core.Entities
{
    public class Booking
    {
        public Guid Id { get; set; }
        public Guid EventId { get; set; }
        public Guid? UserId { get; set; } 
        public Guid? GuestBookingId { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public DateTimeOffset BookingDate { get; set; }
        public DateTimeOffset? ChargedDate { get; set; }
        public string? PaymentIntentId { get; set; }

        public ICollection<BookingSeat> BookingSeats { get; set; } = new List<BookingSeat>();
        [Timestamp]
        public byte[] RowVersion { get; set; }
    }

    public enum PaymentStatus
    {
        Pending,
        Paid,
        Failed,
        Refunded
    }

}
