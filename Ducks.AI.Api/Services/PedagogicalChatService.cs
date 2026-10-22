using Ducks.AI.Api.Models;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace Ducks.AI.Api.Services;

public class PedagogicalChatService
{
    private readonly ChatClient _chatClient;
    private readonly OpenAIOptions _options;
    private readonly FaqService _faqService;
    private readonly ILogger<PedagogicalChatService> _logger;

    public PedagogicalChatService(
        IOptions<OpenAIOptions> options, 
        FaqService faqService,
        ILogger<PedagogicalChatService> logger)
    {
        _options = options.Value;
        _faqService = faqService;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("OpenAI API Key não configurada no backend.");
        }

        _chatClient = new ChatClient(_options.Model, _options.ApiKey);
    }

    public async Task<string> ObterRespostaPedagogicaAsync(string mensagemAluno, List<string>? historicoAnterior = null)
    {
        // 1. Tenta responder direto pela base local de FAQ (economia de tokens e custo zero)
        var respostaPronta = _faqService.BuscarRespostaPronta(mensagemAluno);
        if (!string.IsNullOrEmpty(respostaPronta))
        {
            _logger.LogInformation("Resposta atendida via base local de FAQ (Custo OpenAI: 0 tokens).");
            return respostaPronta;
        }

        // 2. Se for dúvida pedagógica/programação, aciona o modelo gpt-4o-mini da OpenAI
        var systemPrompt = @"
            Você é o Professor Virtual do ecossistema educacional Ducks. 
            Sua missão é auxiliar estudantes de tecnologia com explicações claras, didáticas e objetivas. 
            Nunca forneça códigos prontos de tarefas avaliativas sem explicar a lógica por trás. 
            Incentive o autodesenvolvimento e mantenha um tom encorajador. 
            Proibido participar de qualquer sistema de ranking ou competição.";

        var messages = new List<ChatMessage> { new SystemChatMessage(systemPrompt) };

        if (historicoAnterior != null && historicoAnterior.Count > 0)
        {
            var mensagensLimitadas = historicoAnterior.TakeLast(_options.MaxContextMessages);
            foreach (var hist in mensagensLimitadas)
            {
                messages.Add(new UserChatMessage(hist));
            }
        }

        messages.Add(new UserChatMessage(mensagemAluno));

        var chatOptions = new ChatCompletionOptions
        {
            MaxOutputTokenCount = _options.MaxOutputTokens
        };

        var response = await _chatClient.CompleteChatAsync(messages, chatOptions);

        _logger.LogInformation("Dúvida pedagógica processada via OpenAI gpt-4o-mini.");

        return response.Value.Content[0].Text;
    }
}