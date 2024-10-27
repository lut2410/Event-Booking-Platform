using System;
using System.Threading.Tasks;

namespace Bookings.Application.Interfaces
{
    public interface IRedisFraudDetectionService
    {
        /// <summary>
        /// Records a failed attempt for the specified user. Sets an expiration on the attempt counter if it’s newly created.
        /// </summary>
        /// <param name="userId">The user ID for whom the failed attempt is being recorded.</param>
        Task RecordFailedAttemptAsync(Guid userId);

        /// <summary>
        /// Checks if the specified user is blocked based on the maximum allowed failed attempts.
        /// </summary>
        /// <param name="userId">The user ID to check for blocking status.</param>
        /// <returns>True if the user is blocked, otherwise false.</returns>
        Task<bool> IsUserBlockedAsync(Guid userId);

        /// <summary>
        /// Clears the failed attempt counter for the specified user after a successful action.
        /// </summary>
        /// <param name="userId">The user ID for whom to clear the failed attempts.</param>
        Task ClearFailedAttemptsAsync(Guid userId);
    }
}
