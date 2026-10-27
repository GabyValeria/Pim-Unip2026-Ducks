using Microsoft.AspNetCore.Mvc;

namespace Ducks.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new 
        { 
            status = "Healthy", 
            timestamp = DateTime.UtcNow, 
            project = "Ducks API (.NET 10.0)" 
        });
    }
}