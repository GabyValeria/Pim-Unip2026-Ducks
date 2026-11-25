using Ducks.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ducks.Api.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController(DucksDbContext context, ILogger<HealthController> logger) : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        status = "Healthy",
        timestamp = DateTime.UtcNow,
        project = "Ducks API (.NET 10.0)"
    });


    [HttpGet("ready")]
    public async Task<IActionResult> Ready(CancellationToken cancellationToken)
    {
         var connection = context.Database.GetDbConnection();

        try
        {
            await connection.OpenAsync(cancellationToken);

            await connection.CloseAsync();

           return Ok(new
            {
                status = "Ready",
                database = "Available",
               timestamp = DateTime.UtcNow
            });
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
               "ERRO REAL AO CONECTAR AO AZURE SQL."
            );

           return StatusCode(503, new
           {
               status = "NotReady",
               database = "Unavailable",
               message = "Não foi possível conectar ao banco."
           });
        }
    }

}
