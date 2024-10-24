using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace EventManagement.IntegrationTests.Controllers
{
    public class EventManagamentControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public EventManagamentControllerIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Get_Event_ShouldReturnOk()
        {
            // Arrange

            // Act
            var response = await _client.GetAsync("/api/event");

            // Assert
            response.EnsureSuccessStatusCode();
        }

    }
}
