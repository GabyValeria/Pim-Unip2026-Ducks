using Ducks.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ducks.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/admin/gamification")]
public class AdminGamificationController : ControllerBase
{
    private readonly DucksDbContext _context;

    public AdminGamificationController(DucksDbContext context)
    {
        _context = context;
    }

    [HttpGet("badges")]
    public async Task<IActionResult> GetBadges()
    {
        return Ok(new List<object>());
    }

    [HttpGet("challenges")]
    public async Task<IActionResult> GetChallenges()
    {
        return Ok(new List<object>());
    }

    [HttpPost("xp/adjust")]
    public async Task<IActionResult> AdjustXp([FromBody] UserXpAdjustmentDto dto)
    {
        var xpRegistro = await _context.XPUsuarios.FirstOrDefaultAsync(x => x.UsuarioID == dto.UsuarioID);
        if (xpRegistro == null)
        {
            xpRegistro = new Ducks.Domain.Entities.XPUsuario
            {
                UsuarioID = dto.UsuarioID,
                TotalXP = dto.QuantidadeXP,
                NivelAtualID = 1,
                AtualizadoEm = DateTime.UtcNow
            };
            _context.XPUsuarios.Add(xpRegistro);
        }
        else
        {
            xpRegistro.TotalXP += dto.QuantidadeXP;
            xpRegistro.AtualizadoEm = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return Ok(new { sucesso = true });
    }
}

public class UserXpAdjustmentDto
{
    public int UsuarioID { get; set; }
    public int QuantidadeXP { get; set; }
    public string Motivo { get; set; } = string.Empty;
}