using Bookings.Application.DTOs;
using Bookings.Application.Interfaces;
using Bookings.Core.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bookings.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookingController : ControllerBase
    {
        private readonly ILogger<BookingController> _logger;
        private readonly IBookingService _bookingService;

        public BookingController(ILogger<BookingController> logger, IBookingService bookingService)
        {
            _logger = logger;
            _bookingService = bookingService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BookingDTO>>> GetBookings()
        {
            _logger.LogInformation("Fetching all bookings.");
            var bookings = await _bookingService.GetAllAsync();
            _logger.LogInformation("Successfully retrieved {Count} bookings.", bookings.Count());
            return Ok(bookings);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Booking>> GetBookingById(Guid id)
        {
            _logger.LogInformation("Fetching booking with BookingId: {BookingId}", id);
            var booking = await _bookingService.GetByIdAsync(id);

            if (booking == null)
            {
                _logger.LogWarning("No booking found with BookingId: {BookingId}", id);
                return NotFound();
            }

            _logger.LogInformation("Successfully retrieved booking with BookingId: {BookingId}", id);
            return Ok(booking);
        }

        [HttpPost("reserve")]
        public async Task<ActionResult> ReserveSeats([FromBody] ReserveSeatsRequest request)
        {
            _logger.LogInformation("Attempting to reserve seats for EventId: {EventId}, UserId: {UserId}, SeatIds: {SeatIds}",
                                    request.EventId, request.UserId, string.Join(", ", request.SeatIds));

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for ReserveSeats request.");
                return BadRequest(ModelState);
            }

            try
            {
                var reserved = await _bookingService.ReserveSeatsAsync(request.EventId, request.UserId, request.SeatIds);
                _logger.LogInformation("Successfully reserved seats with BookingId: {BookingId}, Reservation expires at: {ReservationExpiresAt}",
                                        reserved.CreatedBookingId, reserved.ReservationExpiresAt);

                return CreatedAtAction(nameof(ReserveSeats), new { id = reserved.CreatedBookingId, reservationExpiresAt = reserved.ReservationExpiresAt }, reserved);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while reserving seats for UserId: {UserId}, EventId: {EventId}", request.UserId, request.EventId);
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpPost("confirm-payment")]
        public async Task<ActionResult> ConfirmPayment([FromBody] ConfirmPaymentRequest request)
        {
            _logger.LogInformation("Confirming payment for BookingId: {BookingId}, UserId: {UserId}", request.BookingId, request.UserId);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for ConfirmPayment request.");
                return BadRequest(ModelState);
            }

            var paymentResult = await _bookingService.ConfirmPaymentAsync(request.BookingId, request.UserId, request.PaymentRequest);

            if (paymentResult.Success)
            {
                _logger.LogInformation("Payment confirmed successfully for BookingId: {BookingId}", request.BookingId);
                return Ok(paymentResult.Message);
            }
            else
            {
                if (paymentResult.Message == "requires_action")
                {
                    _logger.LogWarning("Payment requires further action (3D Secure) for BookingId: {BookingId}", request.BookingId);
                    return BadRequest("Further action is required, such as 3D Secure authentication.");
                }
                else
                {
                    _logger.LogError("Payment failed for BookingId: {BookingId} with message: {Message}", request.BookingId, paymentResult.Message);
                    return BadRequest($"Payment failed: {paymentResult.Message}");
                }
            }
        }

        // For admin, supporter, event cancellations
        [HttpPost("{bookingId}/refund")]
        public async Task<IActionResult> RefundBooking(Guid bookingId, [FromBody] RefundRequest refundRequest)
        {
            _logger.LogInformation("Processing refund for BookingId: {BookingId}", bookingId);

            var result = await _bookingService.RequestRefundAsync(bookingId, refundRequest);

            if (result.Success)
            {
                _logger.LogInformation("Refund processed successfully for BookingId: {BookingId}", bookingId);
                return Ok(result.Message);
            }
            else
            {
                _logger.LogWarning("Refund failed for BookingId: {BookingId} with message: {Message}", bookingId, result.Message);
                return BadRequest(result.Message);
            }
        }

        // Restrict customer refunds
        [HttpPost("{bookingId}/refund/self")]
        public async Task<IActionResult> SelfRefundBooking(Guid bookingId)
        {
            _logger.LogInformation("Processing self-initiated refund for BookingId: {BookingId}", bookingId);

            var result = await _bookingService.SelfRequestRefundAsync(bookingId);

            if (result.Success)
            {
                _logger.LogInformation("Self-refund processed successfully for BookingId: {BookingId}", bookingId);
                return Ok(result.Message);
            }
            else
            {
                _logger.LogWarning("Self-refund failed for BookingId: {BookingId} with message: {Message}", bookingId, result.Message);
                return BadRequest(result.Message);
            }
        }
    }
}
