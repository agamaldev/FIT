using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace FitApi.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
}
