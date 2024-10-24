namespace Bookings.Core.Entities
{
    public class Event
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
        public DateTime Date { get; set; }

        public ICollection<Seat> Seats { get; set; } = new List<Seat>();
    }
}
