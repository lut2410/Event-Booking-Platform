using Bookings.Application.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bookings.Application.Services
{
    public class RedisSeatReservationService : IRedisSeatReservationService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<RedisSeatReservationService> _logger;

        public RedisSeatReservationService(IConnectionMultiplexer redis, ILogger<RedisSeatReservationService> logger)
        {
            _redis = redis;
            _logger = logger;
        }

        private string GetSeatKey(Guid eventId, Guid seatId) => $"reservation:{eventId}:seat:{seatId}";

        public async Task<bool> TryReserveSeatsAsync(Guid eventId, Guid userId, List<Guid> seatIds, TimeSpan ttl)
        {
            var db = _redis.GetDatabase();
            var transaction = db.CreateTransaction();

            _logger.LogInformation("Attempting to reserve seats for EventId: {EventId}, UserId: {UserId}, TTL: {TTL}", eventId, userId, ttl);

            foreach (var seatId in seatIds)
            {
                var seatKey = GetSeatKey(eventId, seatId);

                var isLocked = await db.LockTakeAsync(seatKey, userId.ToString(), ttl);
                if (!isLocked)
                {
                    _logger.LogWarning("Failed to lock seat {SeatId} for EventId: {EventId} by UserId: {UserId}. Releasing all reserved seats.", seatId, eventId, userId);
                    await ReleaseSeatsAsync(eventId, userId, seatIds);
                    return false;
                }
                else
                {
                    _logger.LogInformation("Successfully locked seat {SeatId} for EventId: {EventId} by UserId: {UserId}", seatId, eventId, userId);
                }
            }

            bool transactionResult = await transaction.ExecuteAsync();
            _logger.LogInformation("Transaction execution for seat reservation {Result} for EventId: {EventId}, UserId: {UserId}", transactionResult ? "succeeded" : "failed", eventId, userId);
            return transactionResult;
        }

        public async Task ConfirmReservationAsync(Guid eventId, List<Guid> seatIds)
        {
            var db = _redis.GetDatabase();

            _logger.LogInformation("Confirming reservation for EventId: {EventId}, Seats: {SeatIds}", eventId, string.Join(", ", seatIds));

            foreach (var seatId in seatIds)
            {
                var seatKey = GetSeatKey(eventId, seatId);

                bool deleted = await db.KeyDeleteAsync(seatKey);
                if (deleted)
                {
                    _logger.LogInformation("Deleted reservation key for SeatId: {SeatId} in EventId: {EventId}", seatId, eventId);
                }
                else
                {
                    _logger.LogWarning("Failed to delete reservation key for SeatId: {SeatId} in EventId: {EventId}", seatId, eventId);
                }
            }
        }

        public async Task ReleaseSeatsAsync(Guid eventId, Guid userId, List<Guid> seatIds)
        {
            var db = _redis.GetDatabase();

            _logger.LogInformation("Releasing seats for EventId: {EventId}, UserId: {UserId}, Seats: {SeatIds}", eventId, userId, string.Join(", ", seatIds));

            foreach (var seatId in seatIds)
            {
                var seatKey = GetSeatKey(eventId, seatId);
                bool released = await db.LockReleaseAsync(seatKey, userId.ToString());

                if (released)
                {
                    _logger.LogInformation("Successfully released lock for SeatId: {SeatId} in EventId: {EventId} by UserId: {UserId}", seatId, eventId, userId);
                }
                else
                {
                    _logger.LogWarning("Failed to release lock for SeatId: {SeatId} in EventId: {EventId} by UserId: {UserId}", seatId, eventId, userId);
                }
            }
        }
    }
}
