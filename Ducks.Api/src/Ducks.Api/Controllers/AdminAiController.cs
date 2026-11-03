using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Ducks.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/admin/ai")]
public class AdminAiController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public AdminAiController(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    [HttpPost("automate")]
    public async Task<IActionResult> Automate([FromBody] AiPromptRequestDto request)
    {
        var apiKey = _configuration["OpenAI:ApiKey"];
        
        // Se a chave não estiver configurada, retorna erro amigável
        if (string.IsNullOrEmpty(apiKey) || apiKey == "SUA_CHAVE_DA_OPENAI_AQUI")
        {
            return BadRequest(new AiResponseDto 
            { 
                Sucesso = false, 
                MensagemErro = "Chave da API OpenAI não configurada no backend (appsettings.json)." 
            });
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";

            var payload = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "system", content = $"Você é um assistente de inteligência artificial sênior para administradores do sistema educacional Ducks. Tarefa administrativa solicitada: {request.TarefaAdmin}" },
                    new { role = "user", content = request.Contexto }
                },
                temperature = 0.7
            };

            var response = await client.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", payload);

            if (!response.IsSuccessStatusCode)
            {
                var errorDetails = await response.Content.ReadAsStringAsync();
                return BadRequest(new AiResponseDto 
                { 
                    Sucesso = false, 
                    MensagemErro = $"Erro na API OpenAI: {response.StatusCode} - {errorDetails}" 
                });
            }

            var openAiResult = await response.Content.ReadFromJsonAsync<OpenAiChatResponseDto>();
            var generatedText = openAiResult?.Choices?[0]?.Message?.Content?.Trim() ?? "Nenhuma resposta gerada pela IA.";

            return Ok(new AiResponseDto
            {
                Sucesso = true,
                ResultadoGerado = generatedText
            });
        }
        catch (System.Exception ex)
        {
            return StatusCode(500, new AiResponseDto 
            { 
                Sucesso = false, 
                MensagemErro = $"Erro interno ao processar IA: {ex.Message}" 
            });
        }
    }
}

// DTOs auxiliares para comunicação com o OpenAI
public class AiPromptRequestDto
{
    public string Contexto { get; set; } = string.Empty;
    public string TarefaAdmin { get; set; } = string.Empty;
}

public class AiResponseDto
{
    public string ResultadoGerado { get; set; } = string.Empty;
    public bool Sucesso { get; set; }
    public string MensagemErro { get; set; } = string.Empty;
}

public class OpenAiChatResponseDto
{
    public Choice[]? Choices { get; set; }
}

public class Choice
{
    public Message? Message { get; set; }
}

public class Message
{
    public string? Content { get; set; }
}