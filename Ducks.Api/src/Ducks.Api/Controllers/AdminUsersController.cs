using Ducks.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Ducks.Domain.Entities;

namespace Ducks.Api.Controllers;

[Authorize(Policy = "AdminOnly")]
[ApiController]
[Route("api/admin/users")]
public class AdminUsersController(DucksDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = context.Usuarios.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(u => u.NomeUsuario.Contains(term) || u.Email.Contains(term));
        }
        var total = await query.CountAsync(cancellationToken);
        var users = await query.OrderBy(u => u.UsuarioID)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(u => new { u.UsuarioID, u.NomeUsuario, u.Email, IsBlocked = !u.EstaAtivo, u.CriadoEm })
            .ToListAsync(cancellationToken);
        var ids = users.Select(u => u.UsuarioID).ToArray();
        var roles = await context.Database.SqlQueryRaw<UserRoleRow>(
            "SELECT up.UsuarioID, p.NomePapel FROM dbo.UsuarioPapeis up INNER JOIN dbo.Papeis p ON p.PapelID = up.PapelID WHERE up.UsuarioID IN (SELECT value FROM STRING_SPLIT({0}, ','))",
            string.Join(',', ids)).ToListAsync(cancellationToken);
        var roleMap = roles.GroupBy(x => x.UsuarioID).ToDictionary(g => g.Key, g => g.Select(x => x.NomePapel).Distinct().ToArray());
        var items = users.Select(u => new { u.UsuarioID, u.NomeUsuario, u.Email, Roles = roleMap.GetValueOrDefault(u.UsuarioID, []), u.IsBlocked, u.CriadoEm });
        return Ok(new { page, pageSize, total, items });
    }

    [HttpPut("{id:int}/block")]
    public async Task<IActionResult> ToggleBlock(int id, [FromQuery] bool block, CancellationToken cancellationToken)
    {
        var user = await context.Usuarios.FirstOrDefaultAsync(u => u.UsuarioID == id, cancellationToken);
        if (user is null) return NotFound();
        user.EstaAtivo = !block;
        user.AtualizadoEm = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var adminId))
        {
            context.LogsAuditoria.Add(new LogAuditoria { UsuarioID = adminId, Acao = block ? "USUARIO_BLOQUEADO" : "USUARIO_DESBLOQUEADO", Detalhes = $"UsuarioID: {id}", CriadoEm = DateTime.UtcNow });
            await context.SaveChangesAsync(cancellationToken);
        }
        return NoContent();
    }
}

public sealed class UserRoleRow { public int UsuarioID { get; set; } public string NomePapel { get; set; } = string.Empty; }
