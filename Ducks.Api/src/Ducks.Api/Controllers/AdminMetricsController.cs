using Ducks.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ducks.Api.Controllers;

[Authorize(Policy = "AdminOnly")]
[ApiController]
[Route("api/admin/metrics")]
public class AdminMetricsController(DucksDbContext context) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var totalUsuarios = await context.Usuarios.CountAsync(cancellationToken);
        var usuariosAtivos = await context.Usuarios.CountAsync(u => u.EstaAtivo, cancellationToken);
        var totalTrilhas = await context.TrilhasAprendizagem.CountAsync(cancellationToken);
        var totalAulasCadastradas = await context.Aulas.CountAsync(cancellationToken);
        var totalXp = await context.XPUsuarios.SumAsync(x => (int?)x.TotalXP, cancellationToken) ?? 0;
        var trilhasPendentes = await context.TrilhasAprendizagem.CountAsync(t => t.Status == "PENDENTE", cancellationToken);
        var aulasPendentes = await context.Aulas.CountAsync(a => a.Status == "PENDENTE", cancellationToken);

        return Ok(new
        {
            TotalUsuarios = totalUsuarios,
            TotalUsuariosAtivos = usuariosAtivos,
            TotalTrilhas = totalTrilhas,
            TotalAulasCadastradas = totalAulasCadastradas,
            TotalXpDistribuido = totalXp,
            ItensPendentesModeracao = trilhasPendentes + aulasPendentes
        });
    }
}
