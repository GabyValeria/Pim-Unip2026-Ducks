using OpenAI.Chat;

namespace Ducks.AI.Api.Services;

public class PedagogicalChatService
{
    private readonly ChatClient _chatClient;
    private readonly string _model;

    public PedagogicalChatService(IConfiguration configuration)
    {
        var apiKey = configuration["OpenAISettings:ApiKey"] 
            ?? throw new InvalidOperationException("OpenAI API Key não configurada no backend.");
        
        _model = configuration["OpenAISettings:Model"] ?? "gpt-4o-mini";
        
        // Inicializa o cliente OpenAI oficial de forma isolada no servidor
        _chatClient = new ChatClient(_model, apiKey);
    }

    public async Task<string> ObterRespostaPedagogicaAsync(string mensagemAluno)
    {
        // Prompt de Sistema Centralizado: Define estritamente o comportamento do assistente educacional
        var systemPrompt = @"
            Você é o Professor Virtual do ecossistema educacional Ducks. 
            Sua missão é auxiliar estudantes de tecnologia com explicações claras, didáticas e objetivas. 
            Nunca forneça códigos prontos de tarefas avaliativas sem explicar a lógica por trás. 
            Incentive o autodesenvolvimento e mantenha um tom encorajador. 
            Proibido participar de qualquer sistema de ranking ou competição.";

        var messages = new ChatMessage[]
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(mensagemAluno)
        };

        var response = await _chatClient.CompleteChatAsync(messages);
        
        return response.Value.Content[0].Text;
    }
}