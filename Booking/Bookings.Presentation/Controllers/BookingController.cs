using Azure.Core;
using Bookings.Application.Services;
using Bookings.Core.Entities;
using Bookings.Core.Interfaces.Services;
using Bookings.Presentation.Models;
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
        public async Task<ActionResult<IEnumerable<Booking>>> GetBookings()
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

        [HttpPost]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
        {
            _logger.LogInformation($"Booking");
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var booking = await _bookingService.CreateBookingAsync(request.EventId, request.UserId, request.SeatIds);
                _logger.LogInformation("Successfully added booking with Id: {BookingId}", booking.Id);
                return CreatedAtAction(nameof(GetBookingById), new { id = booking.Id }, booking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while adding a booking for UserId: {UserId}, EventId: {EventId}",
                                 request.UserId, request.EventId);
                throw;
            }
        }
    }
}
