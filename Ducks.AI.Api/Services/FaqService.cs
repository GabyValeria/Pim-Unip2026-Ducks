namespace Ducks.AI.Api.Services;

public class FaqService
{
    private readonly Dictionary<List<string>, string> _faqDatabase = new()
    {
        {
            new List<string> { "como funciona o xp", "ganhar xp", "pontuação", "pontos", "como ganho xp" },
            "No ecossistema Ducks, você ganha XP (Pontos de Experiência) ao responder questões e concluir provas nos módulos educacionais. O XP é acumulado individualmente para você evoluir de nível e desbloquear conquistas!"
        },
        {
            new List<string> { "tem ranking", "ver ranking", "líderes", "posicao", "classificacao" },
            "O Ducks prioriza o seu aprendizado individual e saúde mental! Por isso, **não temos sistema de ranking ou competição entre alunos**. Seu progresso, XP e conquistas são exclusivamente seus."
        },
        {
            new List<string> { "como ganho medalha", "conquistas", "desbloquear conquista", "medalhas" },
            "As conquistas e medalhas são concedidas automaticamente quando você atinge certos marcos, como concluir módulos, acertar sequências de questões ou alcançar novos níveis de XP!"
        },
        {
            new List<string> { "o que e o ducks", "plataforma", "sobre o projeto", "sobre o ducks" },
            "O Ducks é um ecossistema educacional de alta performance e gamificado, focado em auxiliar estudantes de tecnologia com trilhas de aprendizagem, provas interativas e um assistente pedagógico virtual."
        },
        {
            new List<string> { "esqueci a senha", "mudar senha", "alterar senha", "recuperar conta" },
            "Para recuperar ou alterar sua senha, acesse a tela de login na plataforma Web ou Mobile e selecione a opção 'Esqueci minha senha' ou acesse o menu de Perfil do usuário."
        },
        {
            new List<string> { "onde fica o chat", "como usar o chat", "falar com o professor virtual", "assistente de ia", "chat pedagógico" },
            "O chat pedagógico com o Professor Virtual está disponível **exclusivamente na plataforma Web** para alunos autenticados. Ele utiliza inteligência artificial segura para tirar dúvidas de programação e lógica, sem fornecer códigos prontos de provas!"
        },
        {
            new List<string> { "quais aplicativos", "versão mobile", "aplicativo celular", "versão desktop", "onde posso acessar" },
            "O ecossistema Ducks atende você em três clientes independentes: **Web** (com acesso ao chat de IA e estudos), **Desktop** (WPF para computadores) e **Mobile** (React Native/Expo para celulares). Todos utilizam a sua conta unificada via login seguro."
        },
        {
            new List<string> { "como responder prova", "fazer avaliação", "enviar resposta", "resultado da prova" },
            "As provas e questões são validadas de forma transacional e segura diretamente no servidor da API Principal. Ao submeter uma resposta, o sistema calcula o acerto e credita seu XP automaticamente."
        },
        {
            new List<string> { "trilhas", "matérias", "módulos", "conteúdo das aulas", "onde estudar" },
            "As matérias e trilhas de aprendizagem são organizadas em módulos estruturados no sistema. Você pode navegar por elas no seu próprio ritmo, realizando as aulas e questões correspondentes."
        },
        {
            new List<string> { "papéis", "perfil de usuário", "aluno", "moderador", "curador", "administrador" },
            "O Ducks possui quatro perfis de acesso: **Aluno** (estudante com acesso a trilhas, provas e chat web), **Administrador**, **Curador** e **Moderador**, garantindo a segurança, governança e integridade do conteúdo educacional."
        },
        {
            new List<string> { "suporte", "falar com moderador", "problema técnico", "bug", "ajuda" },
            "Caso encontre algum problema técnico ou tenha dúvidas sobre o conteúdo das aulas, procure pelos canais oficiais de suporte da plataforma ou entre em contato direto com ducks_suporte@gmail.com."
        }
    };

    public string? BuscarRespostaPronta(string mensagemAluno)
    {
        if (string.IsNullOrWhiteSpace(mensagemAluno)) return null;

        var mensagemNormalizada = mensagemAluno.ToLowerInvariant();

        foreach (var item in _faqDatabase)
        {
            if (item.Key.Any(palavraChave => mensagemNormalizada.Contains(palavraChave)))
            {
                return item.Value;
            }
        }

        return null; // Nenhuma resposta pronta encontrada -> encaminhar para a OpenAI
    }
}