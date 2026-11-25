namespace Ducks.Application.DTOs;

public sealed class FiltroDesafiosDto : ParametrosPaginacao
{
    /// <summary>disponiveis (padrão: ativos e dentro do período), participando, concluidos ou todos.</summary>
    public string? Situacao { get; set; }
}

public sealed record DesafioEstudanteDto(
    int DesafioID, string Nome, string Descricao, int RecompensaXP, DateTime? InicioEm, DateTime? FimEm,
    string TipoRequisito, int MetaRequisito, int? TrilhaID, int? ConquistaID, string? NomeConquista,
    bool Vigente, string? StatusParticipacao, DateTime? InscritoEm, DateTime? ConcluidoEm);

public sealed record ProgressoDesafioDto(
    int DesafioID, string Status, string TipoRequisito, int Meta, int Atual, decimal Percentual, bool PodeConcluir,
    DateTime InscritoEm, DateTime? ConcluidoEm, DateTime? FimEm);

public sealed record ConclusaoDesafioDto(int DesafioID, int XPGanho, int? ConquistaID, string? NomeConquista, DateTime ConcluidoEm);
