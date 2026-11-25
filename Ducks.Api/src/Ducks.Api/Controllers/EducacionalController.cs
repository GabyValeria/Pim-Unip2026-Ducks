using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using Ducks.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ducks.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/educacional")]
public class EducacionalController(IEducacionalService educacionalService) : ControllerBase
{
    [HttpGet("trilhas")]
    public async Task<IActionResult> ListarTrilhas(CancellationToken cancellationToken)
    {
        return Ok(await educacionalService.ListarTrilhasAsync(cancellationToken));
    }

    [HttpGet("xp/meu")]
    public async Task<IActionResult> ObterMeuXP(CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();
        var xp = await educacionalService.ObterXPUsuarioAsync(userId, cancellationToken);
        return xp is null ? NotFound() : Ok(xp);
    }

    // Compatibilidade administrativa com a rota anterior. Alunos não podem consultar IDs arbitrários.
    [Authorize(Policy = "AdminOnly")]
    [HttpGet("xp/{usuarioId:int}")]
    public async Task<IActionResult> ObterXP(int usuarioId, CancellationToken cancellationToken)
    {
        var xp = await educacionalService.ObterXPUsuarioAsync(usuarioId, cancellationToken);
        return xp is null ? NotFound() : Ok(xp);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost("xp/adicionar")]
    public async Task<IActionResult> AdicionarXP([FromBody] AdicionarXpDto dto, CancellationToken cancellationToken)
    {
        await educacionalService.AdicionarXPAsync(dto.UsuarioID, dto.QuantidadeXP, dto.Motivo, cancellationToken);
        return NoContent();
    }
}

public sealed class AdicionarXpDto
{
    [Range(1, int.MaxValue)] public int UsuarioID { get; set; }
    [Range(-10000, 10000)] public int QuantidadeXP { get; set; }
    [Required, StringLength(250, MinimumLength = 3)] public string Motivo { get; set; } = string.Empty;
}
