using Bookings.Application.Interfaces;
using StackExchange.Redis;

namespace Bookings.Application.Services
{
    public class RedisSeatReservationService: IRedisSeatReservationService
    {
        private readonly IConnectionMultiplexer _redis;

        public RedisSeatReservationService(IConnectionMultiplexer redis)
        {
            _redis = redis;
        }

        private string GetSeatKey(Guid eventId, Guid seatId) => $"reservation:{eventId}:seat:{seatId}";

        public async Task<bool> TryReserveSeatsAsync(Guid eventId, Guid userId, List<Guid> seatIds, TimeSpan ttl)
        {
            var db = _redis.GetDatabase();
            var transaction = db.CreateTransaction();

            foreach (var seatId in seatIds)
            {
                var seatKey = GetSeatKey(eventId, seatId);

                var isLocked = await db.LockTakeAsync(seatKey, userId.ToString(), ttl);
                if (!isLocked)
                {
                    await ReleaseSeatsAsync(eventId, userId, seatIds);
                    return false;
                }
            }

            return await transaction.ExecuteAsync();
        }

        public async Task ConfirmReservationAsync(Guid eventId, List<Guid> seatIds)
        {
            var db = _redis.GetDatabase();

            foreach (var seatId in seatIds)
            {
                var seatKey = GetSeatKey(eventId, seatId);

                await db.KeyDeleteAsync(seatKey);
            }
        }

        public async Task ReleaseSeatsAsync(Guid eventId, Guid userId, List<Guid> seatIds)
        {
            var db = _redis.GetDatabase();

            foreach (var seatId in seatIds)
            {
                var seatKey = GetSeatKey(eventId, seatId);
                await db.LockReleaseAsync(seatKey, userId.ToString());
            }
        }
    }
}
