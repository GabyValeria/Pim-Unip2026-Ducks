using Ducks.Api.Data;
using Ducks.Api.Models;
using Ducks.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ducks.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class EducacionalController : ControllerBase
{
    private readonly DucksDbContext _context;
    private readonly EduDatabaseService _dbService;

    public EducacionalController(DucksDbContext context, EduDatabaseService dbService)
    {
        _context = context;
        _dbService = dbService;
    }

    // --- MATÉRIAS ---
    [HttpGet("materias")]
    public async Task<IActionResult> ListarMaterias()
    {
        var materias = await _context.Set<Materia>().ToListAsync();
        return Ok(materias);
    }

    // --- TRILHAS ---
    [HttpGet("trilhas")]
    public async Task<IActionResult> ListarTrilhas()
    {
        var trilhas = await _context.TrilhasAprendizagem
            .Where(t => t.Status == "APROVADO")
            .ToListAsync();
        return Ok(trilhas);
    }

    // --- MÓDULOS ---
    [HttpGet("trilhas/{trilhaId}/modulos")]
    public async Task<IActionResult> ListarModulosPorTrilha(int trilhaId)
    {
        var modulos = await _context.Set<Modulo>()
            .Where(m => m.TrilhaId == trilhaId)
            .OrderBy(m => m.IndiceOrdem)
            .ToListAsync();
        return Ok(modulos);
    }

    // --- AULAS ---
    [HttpGet("modulos/{moduloId}/aulas")]
    public async Task<IActionResult> ListarAulasPorModulo(int moduloId)
    {
        var aulas = await _context.Set<Aula>()
            .Where(a => a.ModuloId == moduloId && a.Status == "APROVADO")
            .OrderBy(a => a.IndiceOrdem)
            .ToListAsync();
        return Ok(aulas);
    }

    // --- QUESTÕES ---
    [HttpGet("questoes/{materiaId}")]
    public async Task<IActionResult> ListarQuestoesPorMateria(int materiaId)
    {
        var questoes = await _context.Questoes
            .Where(q => q.MateriaId == materiaId && q.Status == "APROVADO")
            .ToListAsync();
        return Ok(questoes);
    }

    // --- SUBMISSÃO TRANSACIONAL DE RESPOSTAS E XP ---
    public record SubmeterRespostaDto(int ProvaUsuarioId, int QuestaoId, int AlternativaId, int UsuarioId);

    [HttpPost("provas/submeter")]
    public async Task<IActionResult> SubmeterResposta([FromBody] SubmeterRespostaDto dto)
    {
        try
        {
            // Executa a SP transacional oficial que valida gabarito e atribui XP no banco
            await _dbService.SubmeterRespostaProvaAsync(dto.ProvaUsuarioId, dto.QuestaoId, dto.AlternativaId);

            // Verifica se o aluno atingiu marcos e ganhou conquistas automaticamente
            await _dbService.VerificarEConcederConquistasAsync(dto.UsuarioId);

            return Ok(new { mensagem = "Resposta processada com sucesso. XP e progresso atualizados de forma segura." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    // --- GAMIFICAÇÃO: XP E NÍVEL (SEM RANKING) ---
    [HttpGet("gamificacao/usuario/{usuarioId}/xp")]
    public async Task<IActionResult> ObterXP(int usuarioId)
    {
        var xpInfo = await _dbService.ObterXPUsuarioAsync(usuarioId);
        if (xpInfo == null) return NotFound(new { mensagem = "Dados de XP não encontrados para este utilizador." });
        return Ok(xpInfo);
    }

    // --- GAMIFICAÇÃO: CONQUISTAS ---
    [HttpGet("gamificacao/usuario/{usuarioId}/conquistas")]
    public async Task<IActionResult> ObterConquistas(int usuarioId)
    {
        var conquistas = await _dbService.ObterConquistasUsuarioAsync(usuarioId);
        return Ok(conquistas);
    }
}