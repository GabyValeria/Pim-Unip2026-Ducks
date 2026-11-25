using Ducks.Application.Services;

namespace Ducks.Infrastructure.Services;

/// <summary>FAQ inicial e local de contingência. Evoluir para catálogo gerenciado no banco/CMS.</summary>
public sealed class FaqService : IFaqService
{
    private static readonly (string[] Keywords, string Answer)[] Entries =
    {
        (new[] { "xp", "experiência", "pontos" }, "Você ganha XP ao concluir aulas e atividades elegíveis. O saldo e o nível são calculados pela Ducks API; não é possível definir seus próprios pontos."),
        (new[] { "senha", "login", "acessar", "entrar" }, "Para acessar sua conta, utilize seu e-mail e senha cadastrados. Se esqueceu a senha, utilize o fluxo de recuperação disponibilizado pela plataforma."),
        (new[] { "trilha", "trilhas", "módulo", "aula" }, "As trilhas organizam o conteúdo em módulos e aulas. Acesse a área de aprendizagem para consultar as trilhas disponíveis e seu progresso."),
        (new[] { "prova", "simulado", "questão", "questões" }, "As avaliações registram suas respostas e o resultado é processado pela plataforma. Evite enviar a mesma resposta repetidamente."),
        (new[] { "nível", "níveis", "conquista", "medalha" }, "Seu nível e suas conquistas evoluem conforme os critérios de XP e marcos definidos pela plataforma.")
    };

    public Task<string> SearchAsync(string question, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalized = question.Trim();
        foreach (var entry in Entries)
        {
            if (entry.Keywords.Any(keyword => normalized.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                return Task.FromResult(entry.Answer);
        }
        return Task.FromResult("No momento, não encontrei uma resposta no FAQ educacional. Tente reformular sua pergunta ou procure o suporte da plataforma.");
    }
}
