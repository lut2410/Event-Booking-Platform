using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using Xunit;
using Bookings.Application.Interfaces;
using Bookings.Application.Services;

namespace Bookings.Application.Tests.Services
{
    public class RedisFraudDetectionServiceTests
    {
        private readonly Mock<IConnectionMultiplexer> _redisConnectionMock;
        private readonly Mock<IDatabase> _redisDbMock;
        private readonly Mock<ILogger<RedisFraudDetectionService>> _loggerMock;
        private readonly IConfiguration _configuration;
        private readonly RedisFraudDetectionService _fraudDetectionService;

        public RedisFraudDetectionServiceTests()
        {
            _redisConnectionMock = new Mock<IConnectionMultiplexer>();
            _redisDbMock = new Mock<IDatabase>();
            _loggerMock = new Mock<ILogger<RedisFraudDetectionService>>();

            _redisConnectionMock.Setup(conn => conn.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                .Returns(_redisDbMock.Object);

            var inMemorySettings = new Dictionary<string, string>
            {
                { "FraudDetection:MaxFailedAttempts", "5" },
                { "FraudDetection:TrackingPeriodInMinutes", "30" }
            };
            _configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();

            _fraudDetectionService = new RedisFraudDetectionService(
                _redisConnectionMock.Object,
                _loggerMock.Object,
                _configuration);
        }

        [Fact]
        public async Task RecordFailedAttemptAsync_ShouldIncrementFailedAttemptsAndSetExpiry()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var failedAttemptKey = $"fraud:failedAttempts:{userId}";

            _redisDbMock.Setup(db => db.StringIncrementAsync(failedAttemptKey, 1, CommandFlags.None))
                .ReturnsAsync(1);
            _redisDbMock.Setup(db => db.KeyExpireAsync(failedAttemptKey, It.IsAny<TimeSpan>(), CommandFlags.None))
                .ReturnsAsync(true);

            // Act
            await _fraudDetectionService.RecordFailedAttemptAsync(userId);

            // Assert
            _redisDbMock.Verify(db => db.StringIncrementAsync(failedAttemptKey, 1, CommandFlags.None), Times.Once);
            _redisDbMock.Verify(db => db.KeyExpireAsync(failedAttemptKey, It.Is<TimeSpan>(t => t == TimeSpan.FromMinutes(30)), CommandFlags.None), Times.Once);
            _loggerMock.Verify(log => log.LogInformation("Recorded failed attempt for UserId: {UserId}. Current AttemptCount: {AttemptCount}", userId, 1), Times.Once);
        }

        [Fact]
        public async Task IsUserBlockedAsync_ShouldReturnTrue_WhenAttemptCountExceedsMaxFailedAttempts()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var failedAttemptKey = $"fraud:failedAttempts:{userId}";

            _redisDbMock.Setup(db => db.StringGetAsync(failedAttemptKey, CommandFlags.None))
                .ReturnsAsync((RedisValue)"5");

            // Act
            var isBlocked = await _fraudDetectionService.IsUserBlockedAsync(userId);

            // Assert
            Assert.True(isBlocked);
            _loggerMock.Verify(log => log.LogWarning("UserId: {UserId} is currently blocked due to {AttemptCount} failed attempts", userId, 5), Times.Once);
        }

        [Fact]
        public async Task IsUserBlockedAsync_ShouldReturnFalse_WhenAttemptCountIsBelowMaxFailedAttempts()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var failedAttemptKey = $"fraud:failedAttempts:{userId}";

            _redisDbMock.Setup(db => db.StringGetAsync(failedAttemptKey, CommandFlags.None))
                .ReturnsAsync((RedisValue)"3");

            // Act
            var isBlocked = await _fraudDetectionService.IsUserBlockedAsync(userId);

            // Assert
            Assert.False(isBlocked);
            _loggerMock.Verify(log => log.LogWarning(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task ClearFailedAttemptsAsync_ShouldDeleteFailedAttemptKey()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var failedAttemptKey = $"fraud:failedAttempts:{userId}";

            _redisDbMock.Setup(db => db.KeyDeleteAsync(failedAttemptKey, CommandFlags.None))
                .ReturnsAsync(true);

            // Act
            await _fraudDetectionService.ClearFailedAttemptsAsync(userId);

            // Assert
            _redisDbMock.Verify(db => db.KeyDeleteAsync(failedAttemptKey, CommandFlags.None), Times.Once);
            _loggerMock.Verify(log => log.LogInformation("Cleared failed attempts for UserId: {UserId}", userId), Times.Once);
        }
    }
}
