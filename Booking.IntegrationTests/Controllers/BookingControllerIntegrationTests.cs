using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using Bookings.Presentation;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace Bookings.IntegrationTests.Controllers
{
    public class BookingControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public BookingControllerIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Post_CreateBooking_ShouldReturnOk()
        {
            // Arrange
            var bookingDto = new
            {
                UserId = 1,
                EventId = 1,
                BookingDate = "2124-10-23T14:30:00+00:00"
            };

            var content = new StringContent(JsonConvert.SerializeObject(bookingDto), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/booking", content);

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task Post_CreateBooking_ShouldReturnBadRequestForInvalidData()
        {
            // Arrange
            var invalidBookingDto = new
            {
                UserId = 0,  // Invalid userId
                EventId = 1,
                BookingDate = "2024-10-23T14:30:00+00:00"
            };

            var content = new StringContent(JsonConvert.SerializeObject(invalidBookingDto), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/booking", content);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
