using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Billetterie.Api.Controllers
{
    [ApiController]
    [Route("api/[Controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet("public")]
        public IActionResult Public() => Ok("Accessible sans authentification");

        [Authorize]
        [HttpGet("secure")]
        public IActionResult Secure()
        {
            var username = User.FindFirst("preferred_username")?.Value;
            var keycloakId = User.FindFirst("sub")?.Value;
            var roles = User.FindAll("realm_access")?.Select(c => c.Value);

            return Ok(new { username, keycloakId });
        }

    }
}
