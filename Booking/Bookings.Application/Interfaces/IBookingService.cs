using Bookings.Application.DTOs;
using Bookings.Core.Entities;

namespace Bookings.Application.Interfaces
{
    public interface IBookingService
    {
        Task<IEnumerable<BookingDTO>> GetAllAsync();
        Task<BookingDTO> GetByIdAsync(Guid bookingId);
        Task<ReservationResult> ReserveSeatsAsync(Guid eventId, Guid userId, List<Guid> seatIds);
        Task<PaymentResult> ConfirmPaymentAsync(Guid bookingId, Guid userId, PaymentRequest paymentRequest);
        Task<PaymentResult> RequestRefundAsync(Guid bookingId, RefundRequest refundRequest);
        Task<PaymentResult> SelfRequestRefundAsync(Guid bookingId);
    }
}
