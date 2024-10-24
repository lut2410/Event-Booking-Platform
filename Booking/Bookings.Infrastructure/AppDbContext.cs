using Bookings.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bookings.Infrastructure
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        //public DbSet<User> Users { get; set; }
        //public DbSet<GuestBooking> GuestBookings { get; set; }
        public DbSet<Seat> Seats { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<BookingSeat> BookingSeats { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Event>()
                .HasMany(e => e.Seats)
                .WithOne(s => s.Event)
                .HasForeignKey(s => s.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BookingSeat>()
                .HasKey(bs => new { bs.BookingId, bs.SeatId });

            modelBuilder.Entity<BookingSeat>()
                    .HasOne(bs => bs.Booking)
                    .WithMany(b => b.BookingSeats)
                    .HasForeignKey(bs => bs.BookingId)
                    .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BookingSeat>()
                .HasOne(bs => bs.Seat)
                .WithMany(s => s.BookingSeats)
                .HasForeignKey(bs => bs.SeatId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Seat>()
                .Property(s => s.Status)
                .HasConversion<string>();


            modelBuilder.Entity<Booking>()
                .Property(b => b.PaymentStatus)
                .HasConversion<string>();

            modelBuilder.Entity<Seat>()
                .HasIndex(s => s.EventId)
                .IsUnique(false);

            modelBuilder.Entity<Booking>()
                .HasIndex(b => b.UserId)
                .IsUnique(false);

            modelBuilder.Entity<Booking>()
                .HasIndex(b => b.GuestBookingId)
                .IsUnique(false);
        }
    }
}
