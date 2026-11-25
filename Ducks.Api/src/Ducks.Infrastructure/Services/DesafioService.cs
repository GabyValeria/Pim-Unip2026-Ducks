using Ducks.Application.DTOs;
using Ducks.Application.Services;
using Ducks.Domain.Entities;
using Ducks.Domain.Exceptions;
using Ducks.Infrastructure.Persistence;
using Ducks.Infrastructure.Services.Comum;
using Microsoft.EntityFrameworkCore;

namespace Ducks.Infrastructure.Services;

public class DesafioService(DucksDbContext context, IEducacionalService educacional) : IDesafioService
{
    private readonly Recompensas _recompensas = new(context, educacional);

    public async Task<ResultadoPaginado<DesafioEstudanteDto>> ListarAsync(int usuarioId, FiltroDesafiosDto filtro, CancellationToken ct = default)
    {
        var agora = DateTime.UtcNow;
        var query = from d in context.Desafios.AsNoTracking()
                    join p in context.ParticipacoesDesafio.AsNoTracking().Where(p => p.UsuarioID == usuarioId) on d.DesafioID equals p.DesafioID into ps
                    from p in ps.DefaultIfEmpty()
                    join c in context.Conquistas.AsNoTracking() on d.ConquistaID equals c.ConquistaID into cs
                    from c in cs.DefaultIfEmpty()
                    select new { d, p, NomeConquista = c != null ? c.Nome : null };
        query = (filtro.Situacao?.Trim().ToLowerInvariant()) switch
        {
            "participando" => query.Where(x => x.p != null && x.p.Status == StatusParticipacao.EmAndamento),
            "concluidos" => query.Where(x => x.p != null && x.p.Status == StatusParticipacao.Concluido),
            "todos" => query.Where(x => x.d.Ativo || x.p != null),
            null or "" or "disponiveis" => query.Where(x => x.d.Ativo && (x.d.InicioEm == null || x.d.InicioEm <= agora) && (x.d.FimEm == null || x.d.FimEm >= agora)),
            _ => throw new BusinessRuleException("Situação inválida. Use: disponiveis, participando, concluidos ou todos.")
        };
        query = filtro.OrdenarPor?.ToLowerInvariant() switch
        {
            "nome" => filtro.Decrescente ? query.OrderByDescending(x => x.d.Nome) : query.OrderBy(x => x.d.Nome),
            "recompensa" => filtro.Decrescente ? query.OrderByDescending(x => x.d.RecompensaXP) : query.OrderBy(x => x.d.RecompensaXP),
            // Padrão: os que terminam antes aparecem primeiro.
            _ => query.OrderBy(x => x.d.FimEm == null).ThenBy(x => x.d.FimEm).ThenBy(x => x.d.DesafioID)
        };
        return await query.Select(x => new DesafioEstudanteDto(x.d.DesafioID, x.d.Nome, x.d.Descricao, x.d.RecompensaXP, x.d.InicioEm, x.d.FimEm,
                x.d.TipoRequisito, x.d.MetaRequisito, x.d.TrilhaID, x.d.ConquistaID, x.NomeConquista,
                x.d.Ativo && (x.d.InicioEm == null || x.d.InicioEm <= agora) && (x.d.FimEm == null || x.d.FimEm >= agora),
                x.p != null ? x.p.Status : null, x.p != null ? x.p.InscritoEm : null, x.p != null ? x.p.ConcluidoEm : null))
            .PaginarAsync(filtro, ct);
    }

    public async Task<ProgressoDesafioDto> ParticiparAsync(int usuarioId, int desafioId, CancellationToken ct = default)
    {
        var desafio = await ObterDesafioAsync(desafioId, ct);
        ExigirVigente(desafio);
        if (await context.ParticipacoesDesafio.AnyAsync(p => p.DesafioID == desafioId && p.UsuarioID == usuarioId, ct))
            throw new ConflictException("Você já participa deste desafio.");
        var participacao = new ParticipacaoDesafio { DesafioID = desafioId, UsuarioID = usuarioId, InscritoEm = DateTime.UtcNow };
        context.ParticipacoesDesafio.Add(participacao);
        context.RegistrarAuditoria(usuarioId, "DESAFIO_PARTICIPACAO", $"DesafioID: {desafioId}");
        await context.SaveChangesAsync(ct);
        return await MontarProgressoAsync(desafio, participacao, ct);
    }

    public async Task<ProgressoDesafioDto> ObterProgressoAsync(int usuarioId, int desafioId, CancellationToken ct = default)
    {
        var desafio = await ObterDesafioAsync(desafioId, ct);
        var participacao = await ObterParticipacaoAsync(usuarioId, desafioId, ct);
        return await MontarProgressoAsync(desafio, participacao, ct);
    }

    public async Task<ConclusaoDesafioDto> ConcluirAsync(int usuarioId, int desafioId, CancellationToken ct = default)
    {
        var desafio = await ObterDesafioAsync(desafioId, ct);
        var participacao = await ObterParticipacaoAsync(usuarioId, desafioId, ct);
        if (participacao.Status == StatusParticipacao.Concluido) throw new ConflictException("Você já concluiu este desafio.");
        ExigirVigente(desafio);
        var progresso = await MontarProgressoAsync(desafio, participacao, ct);
        if (!progresso.PodeConcluir)
            throw new BusinessRuleException($"Requisito ainda não cumprido ({progresso.Atual} de {progresso.Meta}).");

        participacao.Status = StatusParticipacao.Concluido;
        participacao.ConcluidoEm = DateTime.UtcNow;
        context.RegistrarAuditoria(usuarioId, "DESAFIO_CONCLUIDO", $"DesafioID: {desafioId}");
        await context.SaveChangesAsync(ct);
        var xp = await _recompensas.ConcederAsync(usuarioId, OrigemRecompensa.Desafio, desafioId, desafio.RecompensaXP, desafio.ConquistaID, $"Desafio concluído: {desafio.Nome}", ct);
        var nomeConquista = desafio.ConquistaID.HasValue
            ? await context.Conquistas.AsNoTracking().Where(c => c.ConquistaID == desafio.ConquistaID).Select(c => c.Nome).FirstOrDefaultAsync(ct)
            : null;
        return new ConclusaoDesafioDto(desafioId, xp, nomeConquista is null ? null : desafio.ConquistaID, nomeConquista, participacao.ConcluidoEm.Value);
    }

    // ---------------------------------------------------------------- Auxiliares

    private async Task<Desafio> ObterDesafioAsync(int desafioId, CancellationToken ct) =>
        await context.Desafios.AsNoTracking().FirstOrDefaultAsync(d => d.DesafioID == desafioId, ct)
        ?? throw new ResourceNotFoundException("Desafio não encontrado.");

    private async Task<ParticipacaoDesafio> ObterParticipacaoAsync(int usuarioId, int desafioId, CancellationToken ct) =>
        await context.ParticipacoesDesafio.FirstOrDefaultAsync(p => p.DesafioID == desafioId && p.UsuarioID == usuarioId, ct)
        ?? throw new ResourceNotFoundException("Você não participa deste desafio.");

    private static void ExigirVigente(Desafio d)
    {
        var agora = DateTime.UtcNow;
        if (!d.Ativo) throw new BusinessRuleException("Este desafio não está ativo.");
        if (d.InicioEm.HasValue && agora < d.InicioEm.Value) throw new BusinessRuleException("Este desafio ainda não começou.");
        if (d.FimEm.HasValue && agora > d.FimEm.Value) throw new BusinessRuleException("Este desafio já terminou.");
    }

    private async Task<ProgressoDesafioDto> MontarProgressoAsync(Desafio d, ParticipacaoDesafio p, CancellationToken ct)
    {
        var tipo = TiposRequisitoDesafio.Todos.Contains(d.TipoRequisito) ? d.TipoRequisito : TiposRequisitoDesafio.Nenhum;
        var meta = tipo == TiposRequisitoDesafio.Nenhum ? 0 : Math.Max(1, d.MetaRequisito);
        var atual = await ContarAsync(tipo, d.TrilhaID, p.UsuarioID, p.InscritoEm, ct);
        var percentual = meta == 0 ? 100m : Math.Min(100m, Math.Round(atual * 100m / meta, 2));
        var concluido = p.Status == StatusParticipacao.Concluido;
        return new ProgressoDesafioDto(d.DesafioID, p.Status, tipo, meta, atual, concluido ? 100m : percentual,
            !concluido && atual >= meta, p.InscritoEm, p.ConcluidoEm, d.FimEm);
    }

    private async Task<int> ContarAsync(string tipo, int? trilhaId, int usuarioId, DateTime desde, CancellationToken ct)
    {
        switch (tipo)
        {
            case TiposRequisitoDesafio.AulasConcluidas:
                var aulas = context.ProgressosUsuario.AsNoTracking().Where(p => p.UsuarioID == usuarioId && p.ConcluidoEm >= desde);
                if (trilhaId.HasValue)
                    aulas = from p in aulas
                            join a in context.Aulas on p.AulaID equals a.AulaID
                            join m in context.Modulos on a.ModuloID equals m.ModuloID
                            where m.TrilhaID == trilhaId
                            select p;
                return await aulas.CountAsync(ct);
            case TiposRequisitoDesafio.SimuladosAprovados:
                return await (from t in context.ProvasUsuario.AsNoTracking()
                              join pr in context.Provas.AsNoTracking() on t.ProvaID equals pr.ProvaID
                              where t.UsuarioID == usuarioId && t.Status == StatusTentativa.Finalizada && t.ConcluidoEm >= desde
                                    && t.NotaPercentual >= pr.NotaMinimaAprovacao && (trilhaId == null || pr.TrilhaID == trilhaId)
                              select t.ProvaID).Distinct().CountAsync(ct);
            case TiposRequisitoDesafio.TrilhasConcluidas:
                return await context.UsuarioTrilhas.AsNoTracking()
                    .CountAsync(m => m.UsuarioID == usuarioId && m.ConcluidoEm != null && m.ConcluidoEm >= desde && (trilhaId == null || m.TrilhaID == trilhaId), ct);
            case TiposRequisitoDesafio.XPTotal:
                return await context.XPUsuarios.AsNoTracking().Where(x => x.UsuarioID == usuarioId).Select(x => x.TotalXP).FirstOrDefaultAsync(ct);
            default:
                return 0;
        }
    }
}
