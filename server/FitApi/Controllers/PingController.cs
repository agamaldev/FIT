using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitApi.Controllers;

[ApiController]
[Authorize]
[Route("api/ping")]
public class PingController : ApiControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { });
}
