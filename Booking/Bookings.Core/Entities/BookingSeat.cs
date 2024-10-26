
namespace Bookings.Core.Entities
{
    public class BookingSeat
    {
        public Guid BookingId { get; set; }
        public virtual Booking Booking { get; set; }

        public Guid SeatId { get; set; }
        public virtual Seat Seat { get; set; }
    }

}
