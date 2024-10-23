namespace Bookings.Core.Entities
{
    public class Booking
    {
        public int Id { get; set; }
        public string CustomerName { get; set; }
        public DateTime BookingDate { get; set; }
        public string ServiceName { get; set; }
    }
}
