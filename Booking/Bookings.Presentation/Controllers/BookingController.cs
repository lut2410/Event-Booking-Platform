using Azure.Core;
using Bookings.Application.DTOs;
using Bookings.Application.Interfaces;
using Bookings.Application.Services;
using Bookings.Core.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Bookings.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookingController : ControllerBase
    {
        private readonly ILogger<BookingController> _logger;
        private readonly IBookingService _bookingService;

        public BookingController(ILogger<BookingController> logger,
            IBookingService bookingService)
        {
            _logger = logger;
            _bookingService = bookingService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BookingDTO>>> GetBookings()
        {
            var bookings = await _bookingService.GetAllAsync();
            return Ok(bookings);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Booking>> GetBookingById(Guid id)
        {
            var booking = await _bookingService.GetByIdAsync(id);
            if (booking == null)
                return NotFound();

            return Ok(booking);
        }

        [HttpPost("reserve")]
        public async Task<ActionResult> ReserveSeats([FromBody] ReserveSeatsRequest request)
        {
            _logger.LogInformation($"Booking");
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var reserved = await _bookingService.ReserveSeatsAsync(request.EventId, request.UserId, request.SeatIds);
                _logger.LogInformation("Successfully added booking with Id: {BookingId} until {ReservationExpiresAt}", reserved.CreatedBookingId, reserved.ReservationExpiresAt);
                return CreatedAtAction(nameof(ReserveSeats), new { id = reserved.CreatedBookingId, reservationExpiresAt = reserved.ReservationExpiresAt }, reserved);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while adding a booking for UserId: {UserId}, EventId: {EventId}",
                                 request.UserId, request.EventId);
                throw;
            }
        }

        [HttpPost("confirm-payment")]
        public async Task<ActionResult> ConfirmPayment([FromBody] ConfirmPaymentRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var paymentResult = await _bookingService.ConfirmPaymentAsync(request.BookingId, request.UserId, request.PaymentRequest);

            if (paymentResult.Success)
            {
                return Ok(paymentResult.Message);
            }
            else
            {
                if (paymentResult.Message == "requires_action")
                {
                    return BadRequest("Further action is required, such as 3D Secure authentication.");
                }
                else
                {
                    return BadRequest($"Payment failed: {paymentResult}");
                }
            }
        }
    }
}
