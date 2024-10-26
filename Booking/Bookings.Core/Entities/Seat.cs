using System.ComponentModel.DataAnnotations;

namespace Bookings.Core.Entities
{
    public class Seat
    {
        public Guid Id { get; set; }
        public string SeatNumber { get; set; }
        public SeatStatus Status { get; set; } = SeatStatus.Available;

        public Guid EventId { get; set; }
        public Event Event { get; set; }

        public DateTimeOffset? ReservationExpiresAt { get; set; }

        public ICollection<BookingSeat> BookingSeats { get; set; } = new List<BookingSeat>();
        
        [Timestamp]
        public byte[] RowVersion { get; set; }
    }

    public enum SeatStatus
    {
        Available,
        Reserved,
        Booked
    }


}
