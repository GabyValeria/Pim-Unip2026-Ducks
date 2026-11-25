using System.ComponentModel.DataAnnotations;

namespace Ducks.Application.DTOs;

/// <summary>Justificativa da decisão. Obrigatória para rejeitar e para remover.</summary>
public sealed class JustificativaDto
{
    [Required(ErrorMessage = "A justificativa é obrigatória."), StringLength(1000, MinimumLength = 5)]
    public string Justificativa { get; set; } = string.Empty;
}

/// <summary>Comentário opcional na aprovação.</summary>
public sealed class AprovacaoDto
{
    [StringLength(1000)] public string? Comentario { get; set; }
}

public sealed class FiltroPendentesDto : ParametrosPaginacao
{
    /// <summary>TRILHA, MODULO, AULA, QUESTAO ou SIMULADO. Vazio = todos.</summary>
    public string? Tipo { get; set; }
    public string? Busca { get; set; }
}

public sealed class FiltroHistoricoRevisaoDto : ParametrosPaginacao
{
    public string? Tipo { get; set; }
    public int? ConteudoId { get; set; }
    /// <summary>ENVIADO_REVISAO, APROVADO, REJEITADO ou REMOVIDO.</summary>
    public string? Acao { get; set; }
}

public sealed record DecisaoRevisaoDto(string Tipo, int ConteudoID, string StatusAnterior, string StatusNovo, DateTime DataHora);

public sealed record ItemPendenteDto(string Tipo, int ConteudoID, string Titulo, string? Contexto, DateTime EnviadoEm);

public sealed record RevisaoDto(
    int RevisaoID, string Tipo, int ConteudoID, string Acao, string StatusAnterior, string StatusNovo,
    string? Justificativa, int UsuarioID, string? NomeUsuario, DateTime CriadoEm);

public sealed record ItemRevisaoDto(string Tipo, int ConteudoID, string Titulo, string Status, object Detalhes, IReadOnlyList<RevisaoDto> Historico);
