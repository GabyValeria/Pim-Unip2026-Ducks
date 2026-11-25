using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Ducks.Domain.Entities;
using Ducks.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ducks.Api.Controllers;

[Authorize(Policy = "AdminOnly")]
[ApiController]
[Route("api/admin/settings")]
public sealed class AdminSettingsController(DucksDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetSettings(CancellationToken ct) => Ok(await context.ConfiguracoesSistema.AsNoTracking()
        .Where(x => !x.Chave.ToLower().Contains("key") && !x.Chave.ToLower().Contains("secret") && !x.Chave.ToLower().Contains("password") && !x.Chave.ToLower().Contains("token") && !x.Chave.ToLower().Contains("connection"))
        .OrderBy(x => x.Chave).Select(x => new { x.Chave, x.Valor, x.AtualizadoEm }).ToListAsync(ct));

    [HttpPut]
    public async Task<IActionResult> UpdateSetting([FromBody] UpdateSettingDto dto, CancellationToken ct)
    {
        var key = dto.Chave.Trim();
        if (key.Contains("key", StringComparison.OrdinalIgnoreCase) || key.Contains("secret", StringComparison.OrdinalIgnoreCase) || key.Contains("password", StringComparison.OrdinalIgnoreCase) || key.Contains("token", StringComparison.OrdinalIgnoreCase) || key.Contains("connection", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Credenciais e segredos não podem ser alterados por este endpoint. Utilize variáveis de ambiente, User Secrets ou Key Vault.");
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
        var item = await context.ConfiguracoesSistema.FindAsync(new object?[] { key }, ct);
        if (item is null) { item = new ConfiguracaoSistema { Chave = key }; context.ConfiguracoesSistema.Add(item); }
        item.Valor = dto.Valor; item.AtualizadoEm = DateTime.UtcNow; item.AtualizadoPor = userId;
        context.LogsAuditoria.Add(new LogAuditoria { UsuarioID = userId, Acao = "CONFIGURACAO_ATUALIZADA", Detalhes = $"Chave: {key}", CriadoEm = DateTime.UtcNow });
        await context.SaveChangesAsync(ct); return NoContent();
    }
}

public sealed class UpdateSettingDto
{
    [Required, StringLength(100, MinimumLength = 2)] public string Chave { get; set; } = string.Empty;
    [Required, StringLength(4000)] public string Valor { get; set; } = string.Empty;
}
