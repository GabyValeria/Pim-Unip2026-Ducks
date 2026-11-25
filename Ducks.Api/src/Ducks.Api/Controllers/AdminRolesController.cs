using System.ComponentModel.DataAnnotations;
using Ducks.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Ducks.Domain.Entities;

namespace Ducks.Api.Controllers;

[Authorize(Policy = "AdminOnly")]
[ApiController]
[Route("api/admin/roles")]
public class AdminRolesController(DucksDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
    {
        var roles = await context.Database
            .SqlQueryRaw<PapelModel>("SELECT PapelID, NomePapel, Descricao FROM dbo.Papeis")
            .ToListAsync(cancellationToken);
        return Ok(roles);
    }

    [HttpPost("assign")]
    public async Task<IActionResult> AssignRole([FromBody] AssignRoleDto dto, CancellationToken cancellationToken)
    {
        var userExists = await context.Usuarios.AnyAsync(u => u.UsuarioID == dto.UsuarioID, cancellationToken);
        var roleExists = await context.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM dbo.Papeis WHERE PapelID = {dto.PapelID}").SingleAsync(cancellationToken) > 0;
        if (!userExists || !roleExists) return NotFound(new { message = "Usuário ou papel não encontrado." });
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM dbo.UsuarioPapeis WHERE UsuarioID = {dto.UsuarioID};", cancellationToken);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO dbo.UsuarioPapeis (UsuarioID, PapelID, AtribuidoEm) VALUES ({dto.UsuarioID}, {dto.PapelID}, SYSUTCDATETIME());", cancellationToken);
        if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var adminId))
        {
            context.LogsAuditoria.Add(new LogAuditoria { UsuarioID = adminId, Acao = "PAPEL_ATRIBUIDO", Detalhes = $"UsuarioID: {dto.UsuarioID}; PapelID: {dto.PapelID}", CriadoEm = DateTime.UtcNow });
            await context.SaveChangesAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        return NoContent();
    }
}

public sealed class PapelModel
{
    public int PapelID { get; set; }
    public string NomePapel { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
}

public sealed class AssignRoleDto
{
    [Range(1, int.MaxValue)] public int UsuarioID { get; set; }
    [Range(1, int.MaxValue)] public int PapelID { get; set; }
}
