namespace Bookings.Presentation.Models
{
    public class CreateBookingDto
    {
        public int UserId { get; set; }
        public int EventId { get; set; }
        public DateTimeOffset BookingDate { get; set; }
    }
}
