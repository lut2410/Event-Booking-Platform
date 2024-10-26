namespace Bookings.Application.DTOs
{
    public class ReserveSeatsRequest
    {
        public Guid EventId { get; set; }
        public Guid UserId { get; set; }
        public List<Guid> SeatIds { get; set; }
    }
}
