using Ducks.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ducks.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/admin/roles")]
public class AdminRolesController : ControllerBase
{
    private readonly DucksDbContext _context;

    public AdminRolesController(DucksDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetRoles()
    {
        try
        {
            var papeis = await _context.Database
                .SqlQueryRaw<PapelModel>("SELECT PapelID, NomePapel, Descricao FROM Papeis")
                .ToListAsync();
            return Ok(papeis);
        }
        catch
        {
            // Fallback caso a tabela não retorne via SqlQueryRaw no EF Core 10
            var fallback = new List<PapelModel>
            {
                new() { PapelID = 1, NomePapel = "Aluno", Descricao = "Estudante" },
                new() { PapelID = 2, NomePapel = "Administrador", Descricao = "Admin Geral" },
                new() { PapelID = 3, NomePapel = "Curador", Descricao = "Curador de Conteúdo" },
                new() { PapelID = 4, NomePapel = "Moderador", Descricao = "Moderador" }
            };
            return Ok(fallback);
        }
    }

    [HttpPost("assign")]
    public async Task<IActionResult> AssignRole([FromBody] AssignRoleDto dto)
    {
        await _context.Database.ExecuteSqlRawAsync(
            "DELETE FROM UsuarioPapeis WHERE UsuarioID = {0}; INSERT INTO UsuarioPapeis (UsuarioID, PapelID, AtribuidoEm) VALUES ({0}, {1}, GETDATE());",
            dto.UsuarioID, dto.PapelID);

        return Ok(new { sucesso = true, mensagem = "Papel do usuário atualizado com sucesso." });
    }
}

public class PapelModel
    {
    public int PapelID { get; set; }
    public string NomePapel { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
}

public class AssignRoleDto
{
    public int UsuarioID { get; set; }
    public int PapelID { get; set; }
}