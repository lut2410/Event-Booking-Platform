using Bookings.Application.Services;
using Bookings.Core.Entities;
using Bookings.Presentation.Models;
using Microsoft.AspNetCore.Mvc;

namespace Bookings.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookingController : ControllerBase
    {
        private readonly ILogger<BookingController> _logger;
        private readonly BookingService _bookingService;

        public BookingController(ILogger<BookingController> logger,
            BookingService bookingService)
        {
            _logger = logger;
            _bookingService = bookingService;
        }

        [HttpGet]
        public ActionResult<IEnumerable<Booking>> GetBookings()
        {
            return Ok(_bookingService.GetAllBookings());
        }

        [HttpGet("{id}")]
        public ActionResult<Booking> GetBooking(Guid id)
        {
            var booking = _bookingService.GetBookingById(id);
            if (booking == null) return NotFound();
            return Ok(booking);
        }

        [HttpPost]
        public IActionResult CreateBooking([FromBody] CreateBookingDto bookingDto)
        {
            _logger.LogInformation($"Booking");
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var booking = new Booking
            {
                UserId = bookingDto.UserId,
                EventId = bookingDto.EventId,
                BookingDate = bookingDto.BookingDate
            };

            try
            {
                _bookingService.AddBooking(booking);
                _logger.LogInformation("Successfully added booking with Id: {BookingId}", booking.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while adding a booking for UserId: {UserId}, EventId: {EventId}",
                                 booking.UserId, booking.EventId);
                throw;
            }

            return Ok();
        }
    }
}
