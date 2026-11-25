using System.ComponentModel.DataAnnotations;

namespace Ducks.Application.DTOs;

public sealed class AlternativaRequestDto
{
    /// <summary>Informe ao editar uma questão que já foi respondida (mantém a mesma alternativa).</summary>
    public int? AlternativaID { get; set; }
    [Required, StringLength(2000, MinimumLength = 1)] public string Texto { get; set; } = string.Empty;
    public bool Correta { get; set; }
    /// <summary>Por que esta alternativa está certa ou errada (mostrado ao estudante depois de finalizar).</summary>
    [StringLength(1000)] public string? Explicacao { get; set; }
}

public sealed class QuestaoRequestDto
{
    [Required, StringLength(10000, MinimumLength = 5)] public string Enunciado { get; set; } = string.Empty;
    /// <summary>MULTIPLA_ESCOLHA ou VERDADEIRO_FALSO.</summary>
    [Required] public string Tipo { get; set; } = "MULTIPLA_ESCOLHA";
    /// <summary>FACIL, MEDIO ou DIFICIL.</summary>
    public string Dificuldade { get; set; } = "MEDIO";
    [Range(1, 100)] public int Peso { get; set; } = 1;
    /// <summary>Explicação geral da resposta correta.</summary>
    [StringLength(2000)] public string? Explicacao { get; set; }
    [StringLength(150)] public string? Assunto { get; set; }
    [MaxLength(10)] public List<string> Tags { get; set; } = [];
    public int? TrilhaID { get; set; }
    public int? ModuloID { get; set; }
    public int? AulaID { get; set; }
    /// <summary>Opcional: se omitido, vem da trilha.</summary>
    public int? MateriaID { get; set; }
    [Required, MinLength(2), MaxLength(6)] public List<AlternativaRequestDto> Alternativas { get; set; } = [];
}

public sealed class FiltroQuestoesDto : ParametrosPaginacao
{
    public string? Status { get; set; }
    public string? Dificuldade { get; set; }
    public string? Tipo { get; set; }
    public string? Tag { get; set; }
    public string? Assunto { get; set; }
    public int? TrilhaID { get; set; }
    public int? ModuloID { get; set; }
    public int? AulaID { get; set; }
    /// <summary>Busca no enunciado.</summary>
    public string? Busca { get; set; }
}

public sealed record AlternativaDto(int AlternativaID, string Texto, bool Correta, string? Explicacao);

public sealed record QuestaoDto(
    int QuestaoID, string Enunciado, string Tipo, string Dificuldade, int Peso, string? Explicacao, string? Assunto,
    IReadOnlyList<string> Tags, int? TrilhaID, int? ModuloID, int? AulaID, int? MateriaID, string Status,
    DateTime CriadoEm, DateTime AtualizadoEm, IReadOnlyList<AlternativaDto> Alternativas);

public sealed class SimuladoRequestDto
{
    [Required, StringLength(150, MinimumLength = 3)] public string Titulo { get; set; } = string.Empty;
    [StringLength(2000)] public string? Descricao { get; set; }
    public int? TrilhaID { get; set; }
    public int? ModuloID { get; set; }
    /// <summary>Tempo para responder, em minutos. Vazio = sem limite.</summary>
    [Range(1, 600)] public int? TempoLimiteMinutos { get; set; }
    /// <summary>Tentativas por estudante. Vazio = ilimitado.</summary>
    [Range(1, 100)] public int? MaxTentativas { get; set; }
    /// <summary>Nota (0 a 100) para considerar o simulado aprovado.</summary>
    [Range(0, 100)] public int NotaMinimaAprovacao { get; set; } = 60;
    /// <summary>XP máximo do simulado (proporcional à nota; nunca soma mais do que isso por estudante).</summary>
    [Range(0, 10000)] public int RecompensaXP { get; set; } = 50;
    /// <summary>Número de questões desejado (usado no sorteio). Ao definir as questões, passa a ser a quantidade real.</summary>
    [Range(1, 200)] public int NumeroQuestoes { get; set; } = 10;
}

public sealed class DefinirQuestoesDto
{
    /// <summary>IDs na ordem em que aparecerão.</summary>
    [Required, MinLength(1), MaxLength(200)] public List<int> QuestaoIds { get; set; } = [];
}

/// <summary>Sorteio de questões APROVADAS do banco, por filtros.</summary>
public sealed class SortearQuestoesDto
{
    /// <summary>Vazio = usa o NumeroQuestoes do simulado.</summary>
    [Range(1, 200)] public int? Quantidade { get; set; }
    public string? Dificuldade { get; set; }
    public string? Tag { get; set; }
    public string? Assunto { get; set; }
    /// <summary>Vazio = usa a trilha/módulo do simulado.</summary>
    public int? TrilhaID { get; set; }
    public int? ModuloID { get; set; }
    /// <summary>true (padrão) troca as questões atuais; false acrescenta ao final.</summary>
    public bool SubstituirAtuais { get; set; } = true;
}

public sealed class FiltroSimuladosDto : ParametrosPaginacao
{
    public string? Status { get; set; }
    public int? TrilhaID { get; set; }
    public int? ModuloID { get; set; }
    public string? Busca { get; set; }
}

public sealed record QuestaoDoSimuladoDto(int QuestaoID, int Ordem, string Enunciado, string Dificuldade, string Status);

public sealed record SimuladoDto(
    int ProvaID, string Titulo, string? Descricao, string TipoProva, int? TrilhaID, int? ModuloID, int? MateriaID,
    int? TempoLimiteMinutos, int? MaxTentativas, int NotaMinimaAprovacao, int RecompensaXP, int NumeroQuestoes,
    string Status, int TotalTentativas, DateTime CriadoEm, DateTime AtualizadoEm, IReadOnlyList<QuestaoDoSimuladoDto> Questoes);

/// <summary>Visão do estudante: sem questões e sem gabarito.</summary>
public sealed record SimuladoEstudanteDto(
    int ProvaID, string Titulo, string? Descricao, int? TrilhaID, int? ModuloID, int NumeroQuestoes,
    int? TempoLimiteMinutos, int? MaxTentativas, int NotaMinimaAprovacao, int RecompensaXP,
    int TentativasRealizadas, int? TentativasRestantes, decimal? MelhorNota, int? TentativaEmAndamentoID);
