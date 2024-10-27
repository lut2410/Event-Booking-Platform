using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bookings.Application.Interfaces
{
    public interface IRedisSeatReservationService
    {
        /// <summary>
        /// Attempts to reserve seats with a specified TTL.
        /// </summary>
        /// <param name="eventId">The ID of the event.</param>
        /// <param name="userId">The ID of the user attempting to reserve seats.</param>
        /// <param name="seatIds">List of seat IDs to be reserved.</param>
        /// <param name="ttl">Time-to-live for the reservation lock.</param>
        /// <returns>True if all seats are successfully reserved; otherwise, false.</returns>
        Task<bool> TryReserveSeatsAsync(Guid eventId, Guid userId, List<Guid> seatIds, TimeSpan ttl);

        /// <summary>
        /// Confirms the reservation by marking seats as permanently booked.
        /// </summary>
        /// <param name="eventId">The ID of the event.</param>
        /// <param name="seatIds">List of seat IDs to confirm reservation.</param>
        /// <returns>A task representing the async operation.</returns>
        Task ConfirmReservationAsync(Guid eventId, List<Guid> seatIds);

        /// <summary>
        /// Releases previously reserved seats, making them available for other users.
        /// </summary>
        /// <param name="eventId">The ID of the event.</param>
        /// <param name="userId">The ID of the user who reserved the seats.</param>
        /// <param name="seatIds">List of seat IDs to be released.</param>
        /// <returns>A task representing the async operation.</returns>
        Task ReleaseSeatsAsync(Guid eventId, Guid userId, List<Guid> seatIds);
    }
}
