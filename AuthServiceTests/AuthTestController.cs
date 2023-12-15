using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthServiceTests.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        [Authorize] // Kræver JWT-godkendelse
        [HttpGet]
        public IActionResult Get()
        {
            return Ok("You're authorized");
        }
    }
}