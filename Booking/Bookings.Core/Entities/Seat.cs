namespace Bookings.Core.Entities
{
    public class Seat
    {
        public Guid Id { get; set; }
        public string SeatNumber { get; set; }
        public SeatStatus Status { get; set; }

        public Guid EventId { get; set; }
        public Event Event { get; set; }

        public Guid? BookingId { get; set; }
        public Booking? Booking { get; set; }

        public DateTimeOffset? ReservationExpiresAt { get; set; }


        public ICollection<BookingSeat> BookingSeats { get; set; }
    }

    public enum SeatStatus
    {
        Available,
        Reserved,
        Sold
    }


}
