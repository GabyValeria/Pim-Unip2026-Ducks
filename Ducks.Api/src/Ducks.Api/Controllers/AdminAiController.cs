using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ducks.Api.Controllers;

[Authorize(Policy = "AdminOnly")]
[ApiController]
[Route("api/admin/ai")]
public sealed class AdminAiController(
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory) : ControllerBase
{
    [HttpPost("automate")]
    public async Task<IActionResult> Automate(
        [FromBody] AiPromptRequestDto request,
        CancellationToken cancellationToken)
    {
        var apiKey = configuration["OpenAI:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new AiResponseDto
            {
                Sucesso = false,
                MensagemErro = "Integração de IA não configurada no servidor."
            });
        }

        try
        {
            var client = httpClientFactory.CreateClient("OpenAI");
            using var message = new HttpRequestMessage(HttpMethod.Post, "v1/responses");
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            message.Content = JsonContent.Create(new
            {
                model = configuration["OpenAI:Model"] ?? "gpt-6-luna",
                instructions = "Você é um assistente de IA para administradores do sistema educacional Ducks. Seja preciso, profissional e não invente dados do sistema que não estejam no contexto.",
                input = $"Tarefa administrativa solicitada: {request.TarefaAdmin}\n\nContexto fornecido: {request.Contexto}",
                max_output_tokens = 1200,
                store = false
            });

            using var response = await client.SendAsync(message, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new AiResponseDto
                {
                    Sucesso = false,
                    MensagemErro = "O provedor de IA não conseguiu processar a solicitação."
                });
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var generatedText = ExtractOutputText(document.RootElement);
            if (string.IsNullOrWhiteSpace(generatedText))
            {
                return StatusCode(StatusCodes.Status502BadGateway, new AiResponseDto
                {
                    Sucesso = false,
                    MensagemErro = "O provedor de IA retornou uma resposta vazia."
                });
            }

            return Ok(new AiResponseDto { Sucesso = true, ResultadoGerado = generatedText.Trim() });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new AiResponseDto
            {
                Sucesso = false,
                MensagemErro = "O serviço de IA está temporariamente indisponível."
            });
        }
    }

    private static string? ExtractOutputText(JsonElement root)
    {
        if (!root.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array)
            return null;

        var segments = new List<string>();
        foreach (var item in output.EnumerateArray())
        {
            if (!item.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
                continue;
            foreach (var part in content.EnumerateArray())
            {
                if (part.TryGetProperty("type", out var type) && type.GetString() == "output_text" &&
                    part.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
                {
                    var value = text.GetString();
                    if (!string.IsNullOrWhiteSpace(value)) segments.Add(value);
                }
            }
        }
        return segments.Count == 0 ? null : string.Join(Environment.NewLine, segments);
    }
}

public sealed class AiPromptRequestDto
{
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(4000, MinimumLength = 1)]
    public string Contexto { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(500, MinimumLength = 1)]
    public string TarefaAdmin { get; set; } = string.Empty;
}

public sealed class AiResponseDto
{
    public string ResultadoGerado { get; set; } = string.Empty;
    public bool Sucesso { get; set; }
    public string MensagemErro { get; set; } = string.Empty;
}
