using Ducks.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ducks.Api.Controllers;

[Authorize(Policy = "AdminOnly")]
[ApiController]
[Route("api/admin/audit")]
public class AdminAuditController(DucksDbContext context) : ControllerBase
{
    [HttpGet("logs")]
    public async Task<IActionResult> GetLogs(CancellationToken cancellationToken)
    {
        var logs = await context.LogsAuditoria
            .AsNoTracking()
            .OrderByDescending(l => l.CriadoEm)
            .Take(100)
            .Select(l => new
            {
                LogID = l.LogAuditoriaID,
                Usuario = l.UsuarioID.ToString(),
                l.Acao,
                l.Detalhes,
                DataHora = l.CriadoEm
            })
            .ToListAsync(cancellationToken);
        return Ok(logs);
    }
}
