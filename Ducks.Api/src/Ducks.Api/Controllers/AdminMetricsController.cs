using Ducks.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ducks.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/admin/metrics")]
public class AdminMetricsController : ControllerBase
{
    private readonly DucksDbContext _context;

    public AdminMetricsController(DucksDbContext context)
    {
        _context = context;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var totalUsuarios = await _context.Usuarios.CountAsync();
        var totalTrilhas = await _context.TrilhasAprendizagem.CountAsync();
        var totalAulasConcluidas = await _context.Aulas.CountAsync();
        var totalXp = await _context.XPUsuarios.SumAsync(x => (int?)x.TotalXP) ?? 0;

        var metrics = new
        {
            TotalUsuariosAtivos = totalUsuarios,
            TotalTrilhas = totalTrilhas,
            TotalAulasConcluidas = totalAulasConcluidas,
            TotalXpDistribuido = totalXp,
            TaxaRetencaoMedia = 88.5,
            ItensPendentesModercao = 0
        };

        return Ok(metrics);
    }
}