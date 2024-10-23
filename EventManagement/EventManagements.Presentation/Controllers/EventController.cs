using Microsoft.AspNetCore.Mvc;

namespace EventManagements.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventController : ControllerBase
    {

        public EventController()
        {
        }

        [HttpGet]
        public ActionResult Test()
        {
            return Ok();
        }
    }
}
