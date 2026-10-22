using Ducks.AI.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ducks.AI.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ChatPedagogicoController : ControllerBase
{
    private readonly PedagogicalChatService _chatService;
    private readonly IConfiguration _configuration;

    public ChatPedagogicoController(PedagogicalChatService chatService, IConfiguration configuration)
    {
        _chatService = chatService;
        _configuration = configuration;
    }

    public record MensagemDto(string Mensagem, List<string>? Historico = null);

    [HttpPost("perguntar")]
    public async Task<IActionResult> Perguntar([FromBody] MensagemDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Mensagem))
        {
            return BadRequest(new { erro = "A mensagem não pode estar vazia." });
        }

        // Validação estrita do tamanho máximo da mensagem de entrada
        int maxLen = _configuration.GetValue<int>("OpenAISettings:MaxMessageLength", 2000);
        if (dto.Mensagem.Length > maxLen)
        {
            return BadRequest(new { erro = $"A mensagem excede o limite máximo permitido de {maxLen} caracteres." });
        }

        // Extração segura do ID do usuário a partir das claims do JWT (nunca confiar em IDs enviados no corpo da requisição)
        var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Anônimo";

        try
        {
            var resposta = await _chatService.ObterRespostaPedagogicaAsync(dto.Mensagem, dto.Historico);
            
            // Retorno estruturado com metadados de auditoria interna
            return Ok(new 
            {
                usuarioId = usuarioIdClaim,
                resposta,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { erro = "Erro ao processar a solicitação pedagógica.", detalhe = ex.Message });
        }
    }
}