namespace Ducks.Domain.Entities;

/// <summary>
/// Estados do fluxo de revisão de conteúdo (trilhas, módulos, aulas, questões e simulados).
/// Os valores "PENDENTE" e "APROVADO" já eram usados pela API; "PENDENTE" significa "em revisão".
/// </summary>
public static class StatusConteudo
{
    public const string Rascunho = "RASCUNHO";
    public const string EmRevisao = "PENDENTE";
    public const string Aprovado = "APROVADO";
    public const string Rejeitado = "REJEITADO";
    public const string Removido = "REMOVIDO";

    public static readonly string[] Todos = [Rascunho, EmRevisao, Aprovado, Rejeitado, Removido];

    /// <summary>Estados em que o curador ainda pode enviar o item para revisão.</summary>
    public static bool PodeEnviarParaRevisao(string status) => status is Rascunho or Rejeitado;
}

/// <summary>Tipos de conteúdo que passam pelo fluxo de revisão.</summary>
public static class TipoConteudo
{
    public const string Trilha = "TRILHA";
    public const string Modulo = "MODULO";
    public const string Aula = "AULA";
    public const string Questao = "QUESTAO";
    public const string Simulado = "SIMULADO";

    public static readonly string[] Todos = [Trilha, Modulo, Aula, Questao, Simulado];

    /// <summary>Aceita o nome no singular, em maiúsculas ou minúsculas (ex.: "aula", "AULA").</summary>
    public static string? Normalizar(string? tipo)
    {
        var valor = tipo?.Trim().ToUpperInvariant();
        return valor is not null && Todos.Contains(valor) ? valor : null;
    }
}
