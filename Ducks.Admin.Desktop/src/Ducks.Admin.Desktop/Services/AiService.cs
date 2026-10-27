using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Ducks.Admin.Desktop.Models;

namespace Ducks.Admin.Desktop.Services
{
    public interface IAiService
    {
        Task<string> SummarizeModerationQueueAsync(string pendingItemsJson);
        Task<string> GenerateActivitySuggestionsAsync(string courseTopic);
    }

    public class AiService : IAiService
    {
        private readonly HttpClient _httpClient;

        public AiService(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<string> SummarizeModerationQueueAsync(string pendingItemsJson)
        {
            var payload = new AiPromptDto
            {
                Prompt = $"Analise e sumarize os seguintes itens pendentes de moderação, destacando prioridades de risco: {pendingItemsJson}",
                Contexto = "Moderação"
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/admin/ai/summarize", payload);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<AiResponseDto>();
                    return result?.Resposta ?? "Nenhuma resposta gerada pela IA.";
                }
                return "[Modo Mock/Fallback] Resumo de moderação: 3 itens requerem atenção imediata por violação de diretrizes de linguagem.";
            }
            catch
            {
                return "[Modo Offline] IA indisponível no momento. Verifique a conexão com o backend.";
            }
        }

        public async Task<string> GenerateActivitySuggestionsAsync(string courseTopic)
        {
            var payload = new AiPromptDto
            {
                Prompt = $"Gere 3 sugestões criativas de atividades e desafios gamificados para o curso de: {courseTopic}",
                Contexto = "GerenciamentoDeCursos"
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/admin/ai/suggest-activities", payload);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<AiResponseDto>();
                    return result?.Resposta ?? "Nenhuma sugestão gerada.";
                }
                return $"[Modo Mock/Fallback] Sugestões para '{courseTopic}': 1. Quiz interativo de fixação; 2. Desafio prático de código com recompensa em XP; 3. Estudo de caso gamificado.";
            }
            catch
            {
                return "[Modo Offline] Não foi possível conectar ao serviço de IA.";
            }
        }
    }
}