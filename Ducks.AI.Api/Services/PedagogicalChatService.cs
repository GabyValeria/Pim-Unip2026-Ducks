using Ducks.AI.Api.Models;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace Ducks.AI.Api.Services;

public class PedagogicalChatService
{
    private readonly ChatClient _chatClient;
    private readonly OpenAIOptions _options;
    private readonly ILogger<PedagogicalChatService> _logger;

    public PedagogicalChatService(IOptions<OpenAIOptions> options, ILogger<PedagogicalChatService> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("OpenAI API Key não configurada no backend.");
        }

        _chatClient = new ChatClient(_options.Model, _options.ApiKey);
    }

    public async Task<string> ObterRespostaPedagogicaAsync(string mensagemAluno, List<string>? historicoAnterior = null)
    {
        var systemPrompt = @"
            Você é o Professor Virtual do ecossistema educacional Ducks. 
            Sua missão é auxiliar estudantes de tecnologia com explicações claras, didáticas e objetivas. 
            Nunca forneça códigos prontos de tarefas avaliativas sem explicar a lógica por trás. 
            Incentive o autodesenvolvimento e mantenha um tom encorajador. 
            Proibido participar de qualquer sistema de ranking ou competição.";

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt)
        };

        // Adiciona histórico limitado para controlar tokens de entrada
        if (historicoAnterior != null && historicoAnterior.Count > 0)
        {
            var mensagensLimitadas = historicoAnterior.TakeLast(_options.MaxContextMessages);
            foreach (var hist in mensagensLimitadas)
            {
                messages.Add(new UserChatMessage(hist));
            }
        }

        messages.Add(new UserChatMessage(mensagemAluno));

        // Configuração estrita de limites de tokens de saída para controlar custos
        var chatOptions = new ChatCompletionOptions
        {
            MaxOutputTokenCount = _options.MaxOutputTokens
        };

        var response = await _chatClient.CompleteChatAsync(messages, chatOptions);

        // Registro de auditoria e consumo (tokens aproximados informados pela API)
        _logger.LogInformation("Requisição de IA processada com sucesso. Modelo: {Model}, Tokens de Saída Limitados a: {MaxTokens}", 
            _options.Model, _options.MaxOutputTokens);

        return response.Value.Content[0].Text;
    }
}