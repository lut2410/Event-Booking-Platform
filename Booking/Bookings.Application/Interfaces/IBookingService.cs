using Bookings.Application.DTOs;
using Bookings.Core.Entities;

namespace Bookings.Application.Interfaces
{
    public interface IBookingService
    {
        Task<IEnumerable<BookingDTO>> GetAllAsync();
        Task<BookingDTO> GetByIdAsync(Guid bookingId);
        Task<BookingDTO> CreateBookingAsync(Guid eventId, Guid userId, List<Guid> seatIds);
    }
}
