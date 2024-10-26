using Bookings.Core.Entities;
using Bookings.Infrastructure;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BookingService.Infrastructure
{
    public static class AppDbContextSeeder
    {
        public static void Seed(AppDbContext context)
        {
            if (!context.Events.Any())
            {
                var events = LoadDataFromJson<Event>("SeedData/Events.json");
                context.Events.AddRange(events);
            }

            if (!context.Seats.Any())
            {
                var seats = LoadDataFromJson<Seat>("SeedData/Seats.json");
                context.Seats.AddRange(seats);
            }

            if (!context.Bookings.Any())
            {
                var bookings = LoadDataFromJson<Booking>("SeedData/Bookings.json");
                context.Bookings.AddRange(bookings);
            }

            if (!context.BookingSeats.Any())
            {
                var bookingSeats = LoadDataFromJson<BookingSeat>("SeedData/BookingSeats.json");
                context.BookingSeats.AddRange(bookingSeats);
            }

            context.SaveChanges();
        }

        private static List<T> LoadDataFromJson<T>(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Could not find the seed data file at path: {filePath}");
            }

            var jsonData = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<List<T>>(jsonData);
        }
    }
}
