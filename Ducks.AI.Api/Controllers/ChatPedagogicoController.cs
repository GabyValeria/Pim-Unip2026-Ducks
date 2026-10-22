using Ducks.AI.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ducks.AI.Api.Controllers;

[Authorize(Roles = "Aluno")] // Restringe estritamente ao papel de Aluno autenticado via Web
[ApiController]
[Route("api/[controller]")]
public class ChatPedagogicoController : ControllerBase
{
    private readonly PedagogicalChatService _chatService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ChatPedagogicoController> _logger;

    public ChatPedagogicoController(
        PedagogicalChatService chatService, 
        IConfiguration configuration,
        ILogger<ChatPedagogicoController> logger)
    {
        _chatService = chatService;
        _configuration = configuration;
        _logger = logger;
    }

    public record MensagemDto(string Mensagem, List<string>? Historico = null);

    [HttpPost("perguntar")]
    public async Task<IActionResult> Perguntar([FromBody] MensagemDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Mensagem))
        {
            return BadRequest(new { erro = "A mensagem não pode estar vazia." });
        }

        // Validação de limite de caracteres configurado no backend
        int maxLen = _configuration.GetValue<int>("OpenAISettings:MaxMessageLength", 2000);
        if (dto.Mensagem.Length > maxLen)
        {
            return BadRequest(new { erro = $"A mensagem excede o limite máximo permitido de {maxLen} caracteres." });
        }

        // Extração segura da identidade a partir do token JWT emitido pela API Principal
        var usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Desconhecido";
        var emailUsuario = User.FindFirst(ClaimTypes.Email)?.Value ?? "N/A";

        _logger.LogInformation("Requisição de chat pedagógico recebida do aluno {Email} (ID: {Id})", emailUsuario, usuarioId);

        try
        {
            // Processamento isolado na OpenAI (sem permissão de escrita no banco de dados)
            var resposta = await _chatService.ObterRespostaPedagogicaAsync(dto.Mensagem, dto.Historico);

            return Ok(new
            {
                usuarioId,
                resposta,
                timestamp = DateTime.UtcNow
            });
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Tempo limite esgotado ao aguardar resposta da OpenAI para o aluno {Id}.", usuarioId);
            return StatusCode(504, new { erro = "O assistente pedagógico demorou muito para responder. Tente novamente mais tarde." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro crítico ao processar o chat pedagógico para o aluno {Id}.", usuarioId);
            return StatusCode(500, new { erro = "Erro interno ao processar a solicitação de IA." });
        }
    }
}