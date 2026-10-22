using Ducks.AI.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ducks.AI.Api.Controllers;

[Authorize] // Exige autenticação estrita do aluno via Web
[ApiController]
[Route("api/[controller]")]
public class ChatPedagogicoController : ControllerBase
{
    private readonly PedagogicalChatService _chatService;

    public ChatPedagogicoController(PedagogicalChatService chatService)
    {
        _chatService = chatService;
    }

    public record MensagemDto(string Mensagem);

    [HttpPost("perguntar")]
    public async Task<IActionResult> Perguntar([FromBody] MensagemDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Mensagem))
        {
            return BadRequest(new { erro = "A mensagem não pode estar vazia." });
        }

        try
        {
            // Validação de limite de tamanho de mensagem (segurança e controle de custos)
            if (dto.Mensagem.Length > 2000)
            {
                return BadRequest(new { erro = "A mensagem excede o tamanho máximo permitido de 2000 caracteres." });
            }

            var resposta = await _chatService.ObterRespostaPedagogicaAsync(dto.Mensagem);
            return Ok(new { resposta });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { erro = "Erro ao processar a resposta pedagógica.", detalhe = ex.Message });
        }
    }
}