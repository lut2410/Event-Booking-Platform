using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Bookings.Application.Interfaces;

namespace Bookings.Application.Services
{
    public class RedisFraudDetectionService : IRedisFraudDetectionService
    {
        private readonly IDatabase _redisDb;
        private readonly ILogger<RedisFraudDetectionService> _logger;
        private readonly int _maxFailedAttempts;
        private readonly TimeSpan _trackingPeriod;

        public RedisFraudDetectionService(
            IConnectionMultiplexer redisConnection,
            ILogger<RedisFraudDetectionService> logger,
            IConfiguration configuration)
        {
            _redisDb = redisConnection.GetDatabase();
            _logger = logger;
            _maxFailedAttempts = configuration.GetValue<int>("FraudDetection:MaxFailedAttempts", 5);
            _trackingPeriod = TimeSpan.FromMinutes(configuration.GetValue<int>("FraudDetection:TrackingPeriodInMinutes", 30));
        }

        private string GetFailedAttemptKey(Guid userId) => $"fraud:failedAttempts:{userId}";

        public async Task RecordFailedAttemptAsync(Guid userId)
        {
            var failedAttemptKey = GetFailedAttemptKey(userId);
            await _redisDb.StringIncrementAsync(failedAttemptKey);
            await _redisDb.KeyExpireAsync(failedAttemptKey, _trackingPeriod);
            var attemptCount = (int)await _redisDb.StringGetAsync(failedAttemptKey);
            _logger.LogInformation("Recorded failed attempt for UserId: {UserId}. Current AttemptCount: {AttemptCount}", userId, attemptCount);
        }

        public async Task<bool> IsUserBlockedAsync(Guid userId)
        {
            var failedAttemptKey = GetFailedAttemptKey(userId);
            var attemptCount = ((int?)(await _redisDb.StringGetAsync(failedAttemptKey))) ?? 0;

            if (attemptCount >= _maxFailedAttempts)
            {
                _logger.LogWarning("UserId: {UserId} is currently blocked due to {AttemptCount} failed attempts", userId, attemptCount);
                return true;
            }

            return false;
        }

        public async Task ClearFailedAttemptsAsync(Guid userId)
        {
            var failedAttemptKey = GetFailedAttemptKey(userId);
            await _redisDb.KeyDeleteAsync(failedAttemptKey);
            _logger.LogInformation("Cleared failed attempts for UserId: {UserId}", userId);
        }
    }
}
