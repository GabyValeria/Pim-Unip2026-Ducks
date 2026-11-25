using System.ComponentModel.DataAnnotations;
using Ducks.Domain.Entities;
using Ducks.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Ducks.Api.Controllers;

[Authorize(Policy = "ContentManagement")]
[ApiController]
[Route("api/admin/content")]
public class AdminContentController(DucksDbContext context) : ControllerBase
{
    [HttpGet("trilhas")]
    public async Task<IActionResult> GetTrilhas(CancellationToken cancellationToken)
    {
        var tracks = await context.TrilhasAprendizagem.AsNoTracking()
            .OrderBy(t => t.NomeTrilha)
            .Select(t => new { t.TrilhaID, t.NomeTrilha, t.Descricao, t.MateriaID, t.Status, t.AtualizadoEm })
            .ToListAsync(cancellationToken);
        return Ok(tracks);
    }

    [HttpPost("trilhas")]
    public async Task<IActionResult> CreateTrilha([FromBody] CreateTrilhaDto dto, CancellationToken cancellationToken)
    {
        var track = new TrilhaAprendizagem
        {
            NomeTrilha = dto.NomeTrilha.Trim(),
            Descricao = dto.Descricao.Trim(),
            MateriaID = dto.MateriaID,
            Status = "PENDENTE",
            AtualizadoEm = DateTime.UtcNow
        };
        context.TrilhasAprendizagem.Add(track);
        if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var adminId))
            context.LogsAuditoria.Add(new LogAuditoria { UsuarioID = adminId, Acao = "TRILHA_CRIADA", Detalhes = track.NomeTrilha, CriadoEm = DateTime.UtcNow });
        await context.SaveChangesAsync(cancellationToken);
        return Created("/api/admin/content/trilhas", new { track.TrilhaID });
    }

    [HttpDelete("trilhas/{id:int}")]
    public async Task<IActionResult> DeleteTrilha(int id, CancellationToken cancellationToken)
    {
        var track = await context.TrilhasAprendizagem.FindAsync(new object?[] { id }, cancellationToken);
        if (track is null) return NotFound();
        context.TrilhasAprendizagem.Remove(track);
        if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var adminId))
            context.LogsAuditoria.Add(new LogAuditoria { UsuarioID = adminId, Acao = "TRILHA_EXCLUIDA", Detalhes = $"TrilhaID: {id}", CriadoEm = DateTime.UtcNow });
        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

public sealed class CreateTrilhaDto
{
    [Required, StringLength(150, MinimumLength = 3)] public string NomeTrilha { get; set; } = string.Empty;
    [StringLength(2000)] public string Descricao { get; set; } = string.Empty;
    [Range(1, int.MaxValue)] public int MateriaID { get; set; }
}
