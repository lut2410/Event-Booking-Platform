using Bookings.Application.Services;
using Bookings.Core.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Bookings.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookingController : ControllerBase
    {
        private readonly BookingService _bookingService;

        public BookingController(BookingService bookingService)
        {
            _bookingService = bookingService;
        }

        [HttpGet]
        public ActionResult<IEnumerable<Booking>> GetBookings()
        {
            return Ok(_bookingService.GetAllBookings());
        }

        [HttpGet("{id}")]
        public ActionResult<Booking> GetBooking(int id)
        {
            var booking = _bookingService.GetBookingById(id);
            if (booking == null) return NotFound();
            return Ok(booking);
        }

        [HttpPost]
        public ActionResult AddBooking(Booking booking)
        {
            _bookingService.AddBooking(booking);
            return Ok();
        }
    }
}
