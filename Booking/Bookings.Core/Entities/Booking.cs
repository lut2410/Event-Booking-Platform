﻿namespace Bookings.Core.Entities
{
    public class Booking
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int EventId { get; set; }
        public DateTimeOffset BookingDate { get; set; }
    }
}
