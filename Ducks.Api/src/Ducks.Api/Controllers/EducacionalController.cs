using Ducks.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ducks.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class EducacionalController : ControllerBase
{
    private readonly IEducacionalService _educacionalService;

    public EducacionalController(IEducacionalService educacionalService)
    {
        _educacionalService = educacionalService;
    }

    [HttpGet("trilhas")]
    public async Task<IActionResult> ListarTrilhas()
    {
        var trilhas = await _educacionalService.ListarTrilhasAsync();
        return Ok(trilhas);
    }

    [HttpGet("xp/{usuarioId}")]
    public async Task<IActionResult> ObterXP(int usuarioId)
    {
        var xp = await _educacionalService.ObterXPUsuarioAsync(usuarioId);
        if (xp == null) return NotFound(new { mensagem = "Registro de XP não encontrado para o usuário." });
        return Ok(xp);
    }

    [HttpPost("xp/adicionar")]
    public async Task<IActionResult> AdicionarXP([FromBody] AdicionarXpDto dto)
    {
        await _educacionalService.AdicionarXPAsync(dto.UsuarioID, dto.QuantidadeXP, dto.Motivo);
        return Ok(new { mensagem = "XP adicionado com sucesso." });
    }
}

public class AdicionarXpDto
{
    public int UsuarioID { get; set; }
    public int QuantidadeXP { get; set; }
    public string Motivo { get; set; } = string.Empty;
}