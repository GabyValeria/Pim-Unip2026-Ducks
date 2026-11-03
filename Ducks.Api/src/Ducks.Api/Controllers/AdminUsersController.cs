using Ducks.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ducks.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/admin/users")]
public class AdminUsersController : ControllerBase
{
    private readonly DucksDbContext _context;

    public AdminUsersController(DucksDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] string? search)
    {
        var query = _context.Usuarios.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(u => u.NomeUsuario.Contains(search) || u.Email.Contains(search));
        }

        var usuarios = await query
            .Select(u => new
            {
                UsuarioID = u.UsuarioID,
                NomeUsuario = u.NomeUsuario,
                Email = u.Email,
                Role = u.UsuarioID == 23 || u.UsuarioID == 29 ? "Administrador" : 
                       u.UsuarioID == 24 ? "Curador" : 
                       u.UsuarioID == 25 ? "Moderador" : "Aluno",
                IsBlocked = !u.EstaAtivo,
                CriadoEm = u.CriadoEm
            })
            .ToListAsync();

        return Ok(usuarios);
    }

    [HttpPut("{id}/block")]
    public async Task<IActionResult> ToggleBlock(int id, [FromQuery] bool block)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null) return NotFound(new { mensagem = "Usuário não encontrado." });

        usuario.EstaAtivo = !block;
        await _context.SaveChangesAsync();
        
        return Ok(new { sucesso = true });
    }
}