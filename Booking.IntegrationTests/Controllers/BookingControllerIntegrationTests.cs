using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Bookings.Application.DTOs;
using Bookings.Application.Interfaces;
using Bookings.Core.Entities;
using Bookings.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bookings.IntegrationTests.Controllers
{
    public class BookingControllerTests : IClassFixture<BookingWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly WebApplicationFactory<Program> _factory;

        public BookingControllerTests(BookingWebApplicationFactory factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace AppDbContext with InMemoryDatabase for integration tests
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));

                    services.Remove(descriptor);

                    services.AddDbContext<AppDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("BookingTestDb");
                    });
                });
            });

            _client = _factory.CreateClient();
        }

        private async Task<Booking> SeedDatabaseAsync(AppDbContext dbContext)
        {
            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                EventId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                BookingDate = DateTimeOffset.UtcNow,
                PaymentStatus = PaymentStatus.Pending,
                RowVersion = new byte[8],
                BookingSeats = new List<BookingSeat>
        {
            new BookingSeat
            {
                Seat = new Seat { Id = Guid.NewGuid(), SeatNumber = "A1", Status = SeatStatus.Available,
                RowVersion = new byte[8]
                }
            }
        }
            };

            dbContext.Bookings.Add(booking);
            await dbContext.SaveChangesAsync();
            return booking;
        }

        [Fact]
        public async Task GetBookings_ShouldReturnBookings()
        {
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await SeedDatabaseAsync(dbContext);

            var response = await _client.GetAsync("/api/Booking");

            response.EnsureSuccessStatusCode();
            var bookings = await response.Content.ReadFromJsonAsync<IEnumerable<BookingDTO>>();
            Assert.NotEmpty(bookings);
        }

        [Fact]
        public async Task GetBookingById_ShouldReturnBooking_WhenBookingExists()
        {
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var seededBooking = await SeedDatabaseAsync(dbContext);

            var response = await _client.GetAsync($"/api/Booking/{seededBooking.Id}");

            response.EnsureSuccessStatusCode();
            var booking = await response.Content.ReadFromJsonAsync<BookingDTO>();
            Assert.NotNull(booking);
            Assert.Equal(seededBooking.Id, booking.Id);
        }

        [Fact]
        public async Task ReserveSeats_ShouldReturnCreated_WhenReservationSucceeds()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Seed an event and seats
            var eventId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var seatIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

            var seats = seatIds.Select(id => new Seat
            {
                Id = id,
                EventId = eventId,
                SeatNumber = $"A{id}",
                Status = SeatStatus.Available,
                RowVersion = new byte[8]
            }).ToList();

            var eventEntity = new Event
            {
                Id = eventId,
                Name = "Sample Event",
                Location = "HCMC",
                Date = DateTimeOffset.UtcNow.AddDays(10),
                Seats = seats
            };

            dbContext.Events.Add(eventEntity);
            await dbContext.SaveChangesAsync();

            var request = new ReserveSeatsRequest
            {
                EventId = eventId,
                UserId = userId,
                SeatIds = seatIds
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/Booking/reserve", request);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        }

        [Fact]
        public async Task ConfirmPayment_ShouldReturnOk_WhenPaymentSucceeds()
        {
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var booking = await SeedDatabaseAsync(dbContext);
            foreach (var item in booking.BookingSeats)
            {
                item.Seat.Status = SeatStatus.Reserved;
            }
            await dbContext.SaveChangesAsync();

            var paymentRequest = new ConfirmPaymentRequest
            {
                BookingId = booking.Id,
                UserId = booking.UserId ?? Guid.NewGuid(),
                PaymentRequest = new PaymentRequest { Amount = 5000, PaymentMethodId = "pm_card_visa" }
            };

            var response = await _client.PostAsJsonAsync("/api/Booking/confirm-payment", paymentRequest);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var message = await response.Content.ReadAsStringAsync();
            Assert.Contains("Payment successful", message);
        }

        [Fact]
        public async Task RefundBooking_ShouldReturnOk_WhenRefundIsSuccessful()
        {
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var booking = await SeedDatabaseAsync(dbContext);
            booking.PaymentStatus = PaymentStatus.Paid;
            booking.PaymentIntentId = "pi_3QEGMs2eZvKYlo2C1uo5Q88w";
            foreach (var item in booking.BookingSeats)
            {
                item.Seat.Status = SeatStatus.Booked;
            }
            await dbContext.SaveChangesAsync();
            var refundRequest = new RefundRequest
            {
                PaymentIntentId = booking.PaymentIntentId,
                Amount = 1,
                Reason = "requested_by_customer"
            };

            var response = await _client.PostAsJsonAsync($"/api/Booking/{booking.Id}/refund", refundRequest);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var message = await response.Content.ReadAsStringAsync();
            Assert.Contains("Refund successful", message);
        }
    }
}
