using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Ducks.Admin.Desktop.Services;

public class DucksDesktopClientService
{
    private readonly HttpClient _httpClient;
    private string? _jwtToken;

    public DucksDesktopClientService()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://localhost:5109/") // URL da API Principal
        };
    }

    // 1. Autenticação e Login (Obtenção do Token JWT)
    public async Task<bool> LoginAsync(string email, string senha)
    {
        var credenciais = new { Email = email, Senha = senha };
        
        var response = await _httpClient.PostAsJsonAsync("api/auth/login", credenciais);
        if (!response.IsSuccessStatusCode) return false;

        var resultado = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        if (resultado == null || string.IsNullOrEmpty(resultado.Token)) return false;

        _jwtToken = resultado.Token;
        
        // Configura o cabeçalho padrão com o token Bearer para as próximas requisições
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _jwtToken);
        return true;
    }

    // 2. Submissão de Resposta de Prova (Validação Transacional no Servidor)
    public async Task<bool> SubmeterRespostaAsync(int provaUsuarioId, int questaoId, int alternativaId)
    {
        GarantirAutenticacao();

        var payload = new { ProvaUsuarioID = provaUsuarioId, QuestaoID = questaoId, AlternativaID = alternativaId };
        var response = await _httpClient.PostAsJsonAsync("api/provas/submeter", payload);

        return response.IsSuccessStatusCode;
    }

    // 3. Consulta de Progresso e XP Individual (Sem Ranking)
    public async Task<object?> ObterProgressoXpAsync(int usuarioId)
    {
        GarantirAutenticacao();
        return await _httpClient.GetFromJsonAsync<object>($"api/gamificacao/xp/{usuarioId}");
    }

    private void GarantirAutenticacao()
    {
        if (string.IsNullOrEmpty(_jwtToken))
        {
            throw new InvalidOperationException("O usuário precisa estar autenticado no Desktop para executar esta operação.");
        }
    }

    private record LoginResponseDto(string Token, string NomeUsuario, List<string> Papeis);
}