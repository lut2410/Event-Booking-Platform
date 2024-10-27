using Bookings.Application.Interfaces;
using Bookings.Core.Entities;
using Bookings.Core.Interfaces.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Bookings.Application.Services
{

    public class ReservationExpiryJob
    {
        private readonly IRedisSeatReservationService _redisReservationService;
        private readonly ISeatRepository _seatRepository;
        private readonly ILogger<ReservationExpiryJob> _logger;

        public ReservationExpiryJob(
            IRedisSeatReservationService redisReservationService,
            ISeatRepository seatRepository,
            ILogger<ReservationExpiryJob> logger)
        {
            _redisReservationService = redisReservationService;
            _seatRepository = seatRepository;
            _logger = logger;
        }

        public async Task CheckAndReleaseExpiredReservationsAsync()
        {
            var expiredReservations = await _seatRepository.GetExpiredReservationsAsync(DateTimeOffset.Now);
            // make sure release in Redis

            // Update in main database
            var seatIds = expiredReservations.Select(r => r.Id).ToList();
            var seats = await _seatRepository.GetSeatsByIdsAsync(seatIds);
            foreach (var seat in seats)
            {
                if (seat.Status == SeatStatus.Reserved && seat.ReservationExpiresAt <= DateTimeOffset.UtcNow)
                {
                    seat.Status = SeatStatus.Available;
                    seat.ReservationExpiresAt = null;
                }
            }
            await _seatRepository.UpdateSeatsAsync(seats);

            _logger.LogInformation("Released expired reservation for Seats: {seatIds}", seatIds);
        }
    }
}
