namespace Bookings.Presentation.Models
{
    public class CreateBookingRequest
    {
        public Guid EventId { get; set; }
        public List<Guid> SeatIds { get; set; }
        public Guid UserId { get; set; }    //TODO: fetch from user context
    }
}
