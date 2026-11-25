using System.ComponentModel.DataAnnotations;
using Ducks.Domain.Entities;
using Ducks.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Ducks.Api.Controllers;

[Authorize(Policy = "AdminOnly")]
[ApiController]
[Route("api/admin/gamification")]
public sealed class AdminGamificationController(DucksDbContext context) : ControllerBase
{
    [HttpGet("badges")]
    public async Task<IActionResult> GetBadges(CancellationToken ct) => Ok(await context.Conquistas.AsNoTracking().OrderBy(x => x.XPNecessario).Select(x => new { x.ConquistaID, x.Nome, x.Descricao, x.XPNecessario, x.Ativa, TotalConcedidas = context.UsuariosConquistas.Count(u => u.ConquistaID == x.ConquistaID) }).ToListAsync(ct));

    [HttpGet("users/{usuarioId:int}/badges")]
    public async Task<IActionResult> GetUserBadges(int usuarioId, CancellationToken ct)
    {
        var items = await (from uc in context.UsuariosConquistas.AsNoTracking()
                           join c in context.Conquistas.AsNoTracking() on uc.ConquistaID equals c.ConquistaID
                           where uc.UsuarioID == usuarioId
                           orderby uc.ConquistadaEm descending
                           select new { c.ConquistaID, c.Nome, c.Descricao, uc.ConquistadaEm }).ToListAsync(ct);
        return Ok(items);
    }

    [HttpPost("badges")]
    public async Task<IActionResult> CreateBadge([FromBody] ConquistaDto dto, CancellationToken ct)
    {
        var item = new Conquista { Nome = dto.Nome.Trim(), Descricao = dto.Descricao.Trim(), XPNecessario = dto.XPNecessario, Ativa = dto.Ativa };
        context.Conquistas.Add(item);
        await SaveAuditAsync("CONQUISTA_CRIADA", $"Conquista {item.Nome}", ct);
        await context.SaveChangesAsync(ct);
        return Ok(item);
    }

    [HttpPut("badges/{id:int}")]
    public async Task<IActionResult> UpdateBadge(int id, [FromBody] ConquistaDto dto, CancellationToken ct)
    {
        var item = await context.Conquistas.FindAsync(new object?[] { id }, ct);
        if (item is null) return NotFound();
        item.Nome = dto.Nome.Trim(); item.Descricao = dto.Descricao.Trim(); item.XPNecessario = dto.XPNecessario; item.Ativa = dto.Ativa;
        await SaveAuditAsync("CONQUISTA_ATUALIZADA", $"Conquista {id}", ct); await context.SaveChangesAsync(ct); return NoContent();
    }

    [HttpDelete("badges/{id:int}")]
    public async Task<IActionResult> DeleteBadge(int id, CancellationToken ct)
    {
        var item = await context.Conquistas.FindAsync(new object?[] { id }, ct); if (item is null) return NotFound();
        context.Conquistas.Remove(item); await SaveAuditAsync("CONQUISTA_EXCLUIDA", $"Conquista {id}", ct); await context.SaveChangesAsync(ct); return NoContent();
    }

    [HttpGet("challenges")]
    public async Task<IActionResult> GetChallenges(CancellationToken ct) => Ok(await context.Desafios.AsNoTracking().OrderByDescending(x => x.CriadoEm).ToListAsync(ct));

    [HttpPost("challenges")]
    public async Task<IActionResult> CreateChallenge([FromBody] DesafioDto dto, CancellationToken ct)
    {
        if (dto.InicioEm.HasValue && dto.FimEm.HasValue && dto.FimEm <= dto.InicioEm) return BadRequest("FimEm deve ser posterior a InicioEm.");
        var erroRequisito = await ValidarRequisitoAsync(dto, ct); if (erroRequisito is not null) return BadRequest(erroRequisito);
        var item = new Desafio { Nome = dto.Nome.Trim(), Descricao = dto.Descricao.Trim(), RecompensaXP = dto.RecompensaXP, InicioEm = dto.InicioEm, FimEm = dto.FimEm, Ativo = dto.Ativo };
        AplicarRequisito(item, dto);
        context.Desafios.Add(item); await SaveAuditAsync("DESAFIO_CRIADO", $"Desafio {item.Nome}", ct); await context.SaveChangesAsync(ct);
        return Ok(item);
    }

    [HttpPut("challenges/{id:int}")]
    public async Task<IActionResult> UpdateChallenge(int id, [FromBody] DesafioDto dto, CancellationToken ct)
    {
        if (dto.InicioEm.HasValue && dto.FimEm.HasValue && dto.FimEm <= dto.InicioEm) return BadRequest("FimEm deve ser posterior a InicioEm.");
        var erroRequisito = await ValidarRequisitoAsync(dto, ct); if (erroRequisito is not null) return BadRequest(erroRequisito);
        var item = await context.Desafios.FindAsync(new object?[] { id }, ct); if (item is null) return NotFound();
        item.Nome = dto.Nome.Trim(); item.Descricao = dto.Descricao.Trim(); item.RecompensaXP = dto.RecompensaXP; item.InicioEm = dto.InicioEm; item.FimEm = dto.FimEm; item.Ativo = dto.Ativo;
        AplicarRequisito(item, dto);
        await SaveAuditAsync("DESAFIO_ATUALIZADO", $"Desafio {id}", ct); await context.SaveChangesAsync(ct); return NoContent();
    }

    [HttpDelete("challenges/{id:int}")]
    public async Task<IActionResult> DeleteChallenge(int id, CancellationToken ct)
    {
        var item = await context.Desafios.FindAsync(new object?[] { id }, ct); if (item is null) return NotFound();
        context.Desafios.Remove(item); await SaveAuditAsync("DESAFIO_EXCLUIDO", $"Desafio {id}", ct); await context.SaveChangesAsync(ct); return NoContent();
    }

    // Acrescentado: requisito para o estudante concluir o desafio e medalha concedida na conclusão (opcionais).
    private async Task<string?> ValidarRequisitoAsync(DesafioDto dto, CancellationToken ct)
    {
        var tipo = (dto.TipoRequisito ?? TiposRequisitoDesafio.Nenhum).Trim().ToUpperInvariant();
        if (!TiposRequisitoDesafio.Todos.Contains(tipo)) return $"TipoRequisito inválido. Use: {string.Join(", ", TiposRequisitoDesafio.Todos)}.";
        if (tipo != TiposRequisitoDesafio.Nenhum && dto.MetaRequisito < 1) return "Informe MetaRequisito (mínimo 1) para este tipo de requisito.";
        if (dto.TrilhaID.HasValue && !await context.TrilhasAprendizagem.AnyAsync(t => t.TrilhaID == dto.TrilhaID, ct)) return "TrilhaID não encontrada.";
        if (dto.ConquistaID.HasValue && !await context.Conquistas.AnyAsync(c => c.ConquistaID == dto.ConquistaID, ct)) return "ConquistaID não encontrada.";
        return null;
    }

    private static void AplicarRequisito(Desafio item, DesafioDto dto)
    {
        item.TipoRequisito = (dto.TipoRequisito ?? TiposRequisitoDesafio.Nenhum).Trim().ToUpperInvariant();
        item.MetaRequisito = item.TipoRequisito == TiposRequisitoDesafio.Nenhum ? 0 : dto.MetaRequisito;
        item.TrilhaID = dto.TrilhaID;
        item.ConquistaID = dto.ConquistaID;
    }

    private Task SaveAuditAsync(string action, string details, CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) throw new UnauthorizedAccessException();
        context.LogsAuditoria.Add(new LogAuditoria { UsuarioID = userId, Acao = action, Detalhes = details, CriadoEm = DateTime.UtcNow });
        return Task.CompletedTask;
    }
}

public sealed class ConquistaDto
{
    [Required, StringLength(120, MinimumLength = 2)] public string Nome { get; set; } = string.Empty;
    [StringLength(1000)] public string Descricao { get; set; } = string.Empty;
    [Range(0, int.MaxValue)] public int XPNecessario { get; set; }
    public bool Ativa { get; set; } = true;
}

public sealed class DesafioDto
{
    [Required, StringLength(120, MinimumLength = 2)] public string Nome { get; set; } = string.Empty;
    [StringLength(1000)] public string Descricao { get; set; } = string.Empty;
    [Range(0, int.MaxValue)] public int RecompensaXP { get; set; }
    public DateTime? InicioEm { get; set; }
    public DateTime? FimEm { get; set; }
    public bool Ativo { get; set; } = true;

    // Acrescentados (opcionais): o que o estudante precisa fazer e a medalha que ganha ao concluir.
    /// <summary>NENHUM (padrão), AULAS_CONCLUIDAS, SIMULADOS_APROVADOS, TRILHAS_CONCLUIDAS ou XP_TOTAL.</summary>
    [StringLength(30)] public string? TipoRequisito { get; set; }
    [Range(0, 100000)] public int MetaRequisito { get; set; }
    /// <summary>Restringe a contagem a uma trilha.</summary>
    public int? TrilhaID { get; set; }
    /// <summary>Medalha concedida ao concluir o desafio.</summary>
    public int? ConquistaID { get; set; }
}
