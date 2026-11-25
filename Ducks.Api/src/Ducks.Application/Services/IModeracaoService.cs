using Ducks.Application.DTOs;

namespace Ducks.Application.Services;

/// <summary>
/// Fluxo de revisão: o Curador envia (RASCUNHO/REJEITADO → PENDENTE); o Moderador ou o Admin
/// aprova (PENDENTE → APROVADO), rejeita com justificativa (PENDENTE → REJEITADO) ou remove
/// conteúdo inadequado (qualquer estado → REMOVIDO). Toda decisão vai para RevisoesConteudo e LogsAuditoria.
/// </summary>
public interface IModeracaoService
{
    Task<DecisaoRevisaoDto> EnviarParaRevisaoAsync(string tipo, int conteudoId, int usuarioId, CancellationToken ct = default);
    Task<DecisaoRevisaoDto> AprovarAsync(string tipo, int conteudoId, int usuarioId, string? comentario, CancellationToken ct = default);
    Task<DecisaoRevisaoDto> RejeitarAsync(string tipo, int conteudoId, int usuarioId, string justificativa, CancellationToken ct = default);
    Task<DecisaoRevisaoDto> RemoverAsync(string tipo, int conteudoId, int usuarioId, string justificativa, CancellationToken ct = default);
    Task<ResultadoPaginado<ItemPendenteDto>> ListarPendentesAsync(FiltroPendentesDto filtro, CancellationToken ct = default);
    Task<ItemRevisaoDto> ObterItemAsync(string tipo, int conteudoId, CancellationToken ct = default);
    Task<ResultadoPaginado<RevisaoDto>> ListarHistoricoAsync(FiltroHistoricoRevisaoDto filtro, CancellationToken ct = default);
}
