namespace Ducks.Application.DTOs;

public sealed class FiltroMatriculasDto : ParametrosPaginacao
{
    /// <summary>EM_ANDAMENTO, CONCLUIDA ou CANCELADA. Vazio = todas.</summary>
    public string? Status { get; set; }
}

public sealed class FiltroHistoricoDto : ParametrosPaginacao
{
    /// <summary>AULA_CONCLUIDA, TRILHA_CONCLUIDA, SIMULADO_FINALIZADO ou DESAFIO_CONCLUIDO. Vazio = todos.</summary>
    public string? Tipo { get; set; }
    public DateTime? De { get; set; }
    public DateTime? Ate { get; set; }
}

public sealed record MatriculaDto(
    int UsuarioTrilhaID, int TrilhaID, string NomeTrilha, string Status, decimal ProgressoPercentual,
    DateTime InscritoEm, DateTime? ConcluidoEm, DateTime? CanceladoEm);

/// <summary>Resultado da matrícula: <c>Criada</c> é falso quando o usuário já estava matriculado (operação idempotente).</summary>
public sealed record ResultadoMatriculaDto(bool Criada, MatriculaDto Matricula);

public sealed record AulaProgressoDto(int AulaID, string Titulo, int IndiceOrdem, bool Concluida, DateTime? ConcluidaEm);

public sealed record SimuladoProgressoDto(int ProvaID, string Titulo, bool Aprovado, decimal? MelhorNota);

public sealed record ModuloProgressoDto(
    int ModuloID, string Titulo, int IndiceOrdem, decimal Percentual, int ItensConcluidos, int TotalItens,
    IReadOnlyList<AulaProgressoDto> Aulas, IReadOnlyList<SimuladoProgressoDto> Simulados);

public sealed record ProgressoTrilhaDto(
    int TrilhaID, string NomeTrilha, string StatusMatricula, decimal Percentual, int ItensConcluidos, int TotalItens,
    DateTime? ConcluidoEm, IReadOnlyList<ModuloProgressoDto> Modulos, IReadOnlyList<SimuladoProgressoDto> SimuladosDaTrilha);

public sealed record ConclusaoAulaDto(
    int AulaID, bool JaConcluida, int XPGanho, decimal ProgressoModulo, decimal ProgressoTrilha,
    bool TrilhaConcluida, int XPConclusaoTrilha);

/// <summary>Resultado do recálculo do progresso de uma trilha (usado também pelas provas).</summary>
public sealed record AtualizacaoProgressoDto(decimal Percentual, bool TrilhaConcluidaAgora, int XPConclusaoTrilha);

public sealed record AtividadeHistoricoDto(string Tipo, int ReferenciaID, string Descricao, DateTime Data, decimal? Nota);

public sealed record RecomendacaoDto(string Tipo, int ReferenciaID, string Titulo, string Motivo);

public sealed record ConquistaResumoDto(int ConquistaID, string Nome, string Descricao, DateTime ConquistadaEm);

public sealed record DashboardEstudanteDto(
    int TotalXP, int NivelAtualID, int TrilhasEmAndamento, int TrilhasConcluidas, int AulasConcluidas,
    int SimuladosRealizados, decimal? MediaSimulados, int DesafiosEmAndamento, int DesafiosConcluidos,
    int TotalConquistas, IReadOnlyList<ConquistaResumoDto> UltimasConquistas,
    IReadOnlyList<MatriculaDto> Matriculas, IReadOnlyList<AtividadeHistoricoDto> UltimasAtividades,
    IReadOnlyList<RecomendacaoDto> Recomendacoes);
