namespace Bookings.Presentation.Models
{
    public class CreateBookingDto
    {
        public Guid UserId { get; set; }
        public Guid EventId { get; set; }
        public DateTimeOffset BookingDate { get; set; }
    }
}
