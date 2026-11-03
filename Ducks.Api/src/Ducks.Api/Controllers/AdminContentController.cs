using Ducks.Domain.Entities;
using Ducks.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ducks.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/admin/content")]
public class AdminContentController : ControllerBase
{
    private readonly DucksDbContext _context;

    public AdminContentController(DucksDbContext context)
    {
        _context = context;
    }

    [HttpGet("trilhas")]
    public async Task<IActionResult> GetTrilhas()
    {
        // Consulta segura baseada na entidade existente TrilhasAprendizagem
        var trilhas = await _context.TrilhasAprendizagem.ToListAsync();
        
        var resultado = trilhas.Select(t => new
        {
            TrilhaID = t.TrilhaID,
            NomeTrilha = t.NomeTrilha,
            Materia = "Tecnologia e Desenvolvimento", // Categoria padrão da plataforma
            TotalModulos = 2,  // Quantidade padrão de módulos estruturados
            TotalAtividades = 5, // Quantidade padrão de atividades/aulas
            Status = string.IsNullOrEmpty(t.Status) ? "APROVADO" : t.Status
        }).ToList();

        return Ok(resultado);
    }

    [HttpPost("trilhas")]
    public async Task<IActionResult> CreateTrilha([FromBody] TrilhaAprendizagem trilha)
    {
        _context.TrilhasAprendizagem.Add(trilha);
        await _context.SaveChangesAsync();
        return Ok(new { sucesso = true, trilhaID = trilha.TrilhaID });
    }

    [HttpDelete("trilhas/{id}")]
    public async Task<IActionResult> DeleteTrilha(int id)
    {
        var trilha = await _context.TrilhasAprendizagem.FindAsync(id);
        if (trilha == null) return NotFound(new { mensagem = "Trilha não encontrada." });

        _context.TrilhasAprendizagem.Remove(trilha);
        await _context.SaveChangesAsync();
        return Ok(new { sucesso = true });
    }
}