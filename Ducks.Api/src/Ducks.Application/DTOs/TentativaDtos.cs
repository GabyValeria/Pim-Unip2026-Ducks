using System.ComponentModel.DataAnnotations;

namespace Ducks.Application.DTOs;

public sealed class RespostaItemDto
{
    [Range(1, int.MaxValue)] public int QuestaoID { get; set; }
    [Range(1, int.MaxValue)] public int AlternativaID { get; set; }
}

/// <summary>Uma ou mais respostas. Pode ser enviado várias vezes até finalizar (a última resposta de cada questão vale).</summary>
public sealed class ResponderDto
{
    [Required, MinLength(1), MaxLength(200)] public List<RespostaItemDto> Respostas { get; set; } = [];
}

public sealed class FiltroTentativasDto : ParametrosPaginacao
{
    public int? ProvaID { get; set; }
    /// <summary>EM_ANDAMENTO ou CONCLUIDA.</summary>
    public string? Status { get; set; }
}

public sealed record AlternativaTentativaDto(int AlternativaID, string Texto);

/// <summary>Questão como o estudante vê durante a prova: sem indicar a correta.</summary>
public sealed record QuestaoTentativaDto(int QuestaoID, int Ordem, string Enunciado, string Tipo,
    IReadOnlyList<AlternativaTentativaDto> Alternativas, int? AlternativaSelecionadaID);

/// <summary>Correção de uma questão (só existe depois de finalizar).</summary>
public sealed record FeedbackQuestaoDto(
    int QuestaoID, int Ordem, string Enunciado, int? AlternativaSelecionadaID, int AlternativaCorretaID, string TextoAlternativaCorreta,
    bool Correta, string? ExplicacaoQuestao, string? ExplicacaoAlternativaSelecionada, string? ExplicacaoAlternativaCorreta);

public sealed record ResultadoTentativaDto(
    int Acertos, int Erros, int EmBranco, decimal NotaPercentual, bool Aprovado, int XPGanho, bool TempoEsgotado,
    IReadOnlyList<FeedbackQuestaoDto> Feedback);

public sealed record TentativaDto(
    int ProvaUsuarioID, int ProvaID, string Titulo, string Status, int NumeroTentativa, DateTime IniciadoEm,
    DateTime? PrazoEm, DateTime? ConcluidoEm, int? TempoRestanteSegundos, int TotalQuestoes, int Respondidas,
    IReadOnlyList<QuestaoTentativaDto> Questoes, ResultadoTentativaDto? Resultado);

/// <summary>Resultado de iniciar: <c>Criada</c> é falso quando já havia uma tentativa em andamento (ela é devolvida).</summary>
public sealed record InicioTentativaDto(bool Criada, TentativaDto Tentativa);

public sealed record RespostaRegistradaDto(int ProvaUsuarioID, int Respondidas, int TotalQuestoes, int? TempoRestanteSegundos);

public sealed record TentativaResumoDto(
    int ProvaUsuarioID, int ProvaID, string Titulo, int NumeroTentativa, string Status, DateTime IniciadoEm,
    DateTime? ConcluidoEm, int TotalQuestoes, int? Acertos, decimal? NotaPercentual, bool? Aprovado, int XPGanho);
