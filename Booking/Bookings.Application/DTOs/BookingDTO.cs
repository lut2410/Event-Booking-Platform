using Bookings.Core.Entities;

namespace Bookings.Application.DTOs
{
    public class BookingDTO
    {
        public Guid Id { get; set; }
        public DateTimeOffset BookingDate { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public Guid EventId { get; set; }
        public List<SeatDTO> Seats { get; set; }
        public Guid? UserId { get; set; }
        public Guid? GuestBookingId { get; set; }
    }

    public class SeatDTO
    {
        public Guid Id { get; set; }
        public string SeatNumber { get; set; }
        public SeatStatus Status { get; set; }
    }
}
