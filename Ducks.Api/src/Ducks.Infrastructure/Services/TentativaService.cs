using Ducks.Application.DTOs;
using Ducks.Application.Services;
using Ducks.Domain.Entities;
using Ducks.Domain.Exceptions;
using Ducks.Infrastructure.Persistence;
using Ducks.Infrastructure.Services.Comum;
using Microsoft.EntityFrameworkCore;

namespace Ducks.Infrastructure.Services;

public class TentativaService(DucksDbContext context, IEducacionalService educacional, IMatriculaService matriculas) : ITentativaService
{
    /// <summary>Folga para respostas enviadas no último segundo (atraso de rede).</summary>
    private static readonly TimeSpan Tolerancia = TimeSpan.FromSeconds(10);
    private readonly Recompensas _recompensas = new(context, educacional);

    public async Task<InicioTentativaDto> IniciarAsync(int usuarioId, int provaId, CancellationToken ct = default)
    {
        var prova = await context.Provas.AsNoTracking().FirstOrDefaultAsync(p => p.ProvaID == provaId && p.Status == StatusConteudo.Aprovado
            && (p.TrilhaID == null || context.TrilhasAprendizagem.Any(t => t.TrilhaID == p.TrilhaID && t.Status == StatusConteudo.Aprovado))
            && (p.ModuloID == null || context.Modulos.Any(m => m.ModuloID == p.ModuloID && m.Status == StatusConteudo.Aprovado)), ct)
            ?? throw new ResourceNotFoundException("Simulado não encontrado.");

        var emAndamento = await context.ProvasUsuario.FirstOrDefaultAsync(t => t.UsuarioID == usuarioId && t.ProvaID == provaId && t.Status == StatusTentativa.EmAndamento, ct);
        if (emAndamento is not null)
        {
            if (!Expirou(emAndamento)) return new InicioTentativaDto(false, await MontarAsync(emAndamento, prova, ct));
            await CorrigirAsync(emAndamento, prova, tempoEsgotado: true, ct);
        }

        var anteriores = await context.ProvasUsuario.CountAsync(t => t.UsuarioID == usuarioId && t.ProvaID == provaId, ct);
        if (prova.MaxTentativas.HasValue && anteriores >= prova.MaxTentativas.Value)
            throw new ConflictException($"Limite de {prova.MaxTentativas} tentativa(s) atingido para este simulado.");
        var questoes = await QuestoesDaProvaAsync(provaId, ct);
        if (questoes.Count == 0) throw new BusinessRuleException("O simulado ainda não tem questões disponíveis.");

        var agora = DateTime.UtcNow;
        var tentativa = new ProvaUsuario
        {
            UsuarioID = usuarioId, ProvaID = provaId, IniciadoEm = agora, Status = StatusTentativa.EmAndamento,
            NumeroTentativa = anteriores + 1, TotalQuestoes = questoes.Count,
            PrazoEm = prova.TempoLimiteMinutos.HasValue ? agora.AddMinutes(prova.TempoLimiteMinutos.Value) : null
        };
        context.ProvasUsuario.Add(tentativa);
        await context.SaveChangesAsync(ct);
        return new InicioTentativaDto(true, await MontarAsync(tentativa, prova, ct));
    }

    public async Task<TentativaDto> ObterAsync(int usuarioId, int tentativaId, CancellationToken ct = default)
    {
        var (tentativa, prova) = await CarregarAsync(usuarioId, tentativaId, ct);
        if (tentativa.Status == StatusTentativa.EmAndamento && Expirou(tentativa))
            await CorrigirAsync(tentativa, prova, tempoEsgotado: true, ct);
        return await MontarAsync(tentativa, prova, ct);
    }

    public async Task<RespostaRegistradaDto> ResponderAsync(int usuarioId, int tentativaId, IReadOnlyList<RespostaItemDto> respostas, CancellationToken ct = default)
    {
        var (tentativa, prova) = await CarregarAsync(usuarioId, tentativaId, ct);
        if (tentativa.Status != StatusTentativa.EmAndamento)
            throw new ConflictException("Esta tentativa já foi finalizada; as respostas não podem ser reenviadas.");
        if (Expirou(tentativa))
        {
            await CorrigirAsync(tentativa, prova, tempoEsgotado: true, ct);
            throw new ConflictException("O tempo do simulado acabou. A tentativa foi finalizada com as respostas já enviadas.");
        }
        if (respostas.Select(r => r.QuestaoID).Distinct().Count() != respostas.Count)
            throw new BusinessRuleException("Há mais de uma resposta para a mesma questão.");

        var questoes = await QuestoesDaProvaAsync(tentativa.ProvaID, ct);
        var questaoIds = respostas.Select(r => r.QuestaoID).ToList();
        var alternativas = await context.AlternativasQuestao.AsNoTracking().Where(a => questaoIds.Contains(a.QuestaoID)).ToListAsync(ct);
        var existentes = await context.RespostasProvasUsuario.Where(r => r.ProvaUsuarioID == tentativaId && questaoIds.Contains(r.QuestaoID)).ToListAsync(ct);
        var agora = DateTime.UtcNow;
        foreach (var item in respostas)
        {
            if (!questoes.Contains(item.QuestaoID)) throw new BusinessRuleException($"A questão {item.QuestaoID} não faz parte deste simulado.");
            var alternativa = alternativas.FirstOrDefault(a => a.AlternativaID == item.AlternativaID && a.QuestaoID == item.QuestaoID)
                ?? throw new BusinessRuleException($"A alternativa {item.AlternativaID} não pertence à questão {item.QuestaoID}.");
            var resposta = existentes.FirstOrDefault(r => r.QuestaoID == item.QuestaoID);
            if (resposta is null)
            {
                resposta = new RespostaProvaUsuario { ProvaUsuarioID = tentativaId, QuestaoID = item.QuestaoID };
                context.RespostasProvasUsuario.Add(resposta);
            }
            resposta.AlternativaSelecionadaID = alternativa.AlternativaID;
            resposta.EhCorreta = alternativa.EhCorreta; // guardado agora, revelado só depois de finalizar
            resposta.RespondidoEm = agora;
        }
        await context.SaveChangesAsync(ct);
        var respondidas = await context.RespostasProvasUsuario.CountAsync(r => r.ProvaUsuarioID == tentativaId, ct);
        return new RespostaRegistradaDto(tentativaId, respondidas, questoes.Count, TempoRestante(tentativa));
    }

    public async Task<TentativaDto> FinalizarAsync(int usuarioId, int tentativaId, CancellationToken ct = default)
    {
        var (tentativa, prova) = await CarregarAsync(usuarioId, tentativaId, ct);
        if (tentativa.Status != StatusTentativa.EmAndamento)
            throw new ConflictException("Esta tentativa já foi finalizada. Consulte o resultado em GET api/educacional/tentativas/{id}.");
        await CorrigirAsync(tentativa, prova, tempoEsgotado: Expirou(tentativa), ct);
        return await MontarAsync(tentativa, prova, ct);
    }

    public async Task<ResultadoPaginado<TentativaResumoDto>> ListarAsync(int usuarioId, FiltroTentativasDto filtro, CancellationToken ct = default)
    {
        var query = from t in context.ProvasUsuario.AsNoTracking()
                    join p in context.Provas.AsNoTracking() on t.ProvaID equals p.ProvaID
                    where t.UsuarioID == usuarioId
                    select new { t, p.Titulo, p.NotaMinimaAprovacao };
        if (filtro.ProvaID.HasValue) query = query.Where(x => x.t.ProvaID == filtro.ProvaID);
        if (!string.IsNullOrWhiteSpace(filtro.Status))
        {
            var status = filtro.Status.Trim().ToUpperInvariant();
            if (status == "FINALIZADA") status = StatusTentativa.Finalizada;
            query = query.Where(x => x.t.Status == status);
        }
        query = filtro.OrdenarPor?.ToLowerInvariant() switch
        {
            "nota" => filtro.Decrescente ? query.OrderByDescending(x => x.t.NotaPercentual) : query.OrderBy(x => x.t.NotaPercentual),
            _ => string.Equals(filtro.Ordem, "asc", StringComparison.OrdinalIgnoreCase) ? query.OrderBy(x => x.t.IniciadoEm) : query.OrderByDescending(x => x.t.IniciadoEm)
        };
        return await query.Select(x => new TentativaResumoDto(x.t.ProvaUsuarioID, x.t.ProvaID, x.Titulo, x.t.NumeroTentativa, x.t.Status,
                x.t.IniciadoEm, x.t.ConcluidoEm, x.t.TotalQuestoes,
                x.t.Status == StatusTentativa.Finalizada ? x.t.Pontuacao : null,
                x.t.Status == StatusTentativa.Finalizada ? x.t.NotaPercentual : null,
                x.t.Status == StatusTentativa.Finalizada ? x.t.NotaPercentual >= x.NotaMinimaAprovacao : null,
                x.t.XPGanho))
            .PaginarAsync(filtro, ct);
    }

    // ---------------------------------------------------------------- Correção

    private async Task CorrigirAsync(ProvaUsuario tentativa, Prova prova, bool tempoEsgotado, CancellationToken ct)
    {
        var questoes = await QuestoesDaProvaAsync(tentativa.ProvaID, ct);
        var pesos = await context.Questoes.AsNoTracking().Where(q => questoes.Contains(q.QuestaoID)).ToDictionaryAsync(q => q.QuestaoID, q => Math.Max(1, q.Peso), ct);
        var respostas = await context.RespostasProvasUsuario.AsNoTracking().Where(r => r.ProvaUsuarioID == tentativa.ProvaUsuarioID && questoes.Contains(r.QuestaoID)).ToListAsync(ct);
        var pesoTotal = pesos.Values.Sum();
        var pesoAcertos = respostas.Where(r => r.EhCorreta).Sum(r => pesos.GetValueOrDefault(r.QuestaoID, 1));

        tentativa.Status = StatusTentativa.Finalizada;
        // Se o tempo acabou, a conclusão fica registrada no prazo (é assim que se sabe que foi encerrada pelo tempo).
        tentativa.ConcluidoEm = tempoEsgotado && tentativa.PrazoEm.HasValue ? tentativa.PrazoEm.Value : DateTime.UtcNow;
        tentativa.TotalQuestoes = questoes.Count;
        tentativa.Pontuacao = respostas.Count(r => r.EhCorreta);
        tentativa.NotaPercentual = pesoTotal == 0 ? 0 : Math.Round(pesoAcertos * 100m / pesoTotal, 2);

        // XP proporcional à nota. Somando todas as tentativas, o estudante nunca passa do RecompensaXP do simulado:
        // refazer só rende a diferença quando a nota melhora.
        var alvo = (int)Math.Round(prova.RecompensaXP * tentativa.NotaPercentual / 100m, MidpointRounding.AwayFromZero);
        var jaGanho = await context.ProvasUsuario.Where(t => t.UsuarioID == tentativa.UsuarioID && t.ProvaID == tentativa.ProvaID && t.ProvaUsuarioID != tentativa.ProvaUsuarioID).SumAsync(t => t.XPGanho, ct);
        var xp = Math.Max(0, alvo - jaGanho);
        context.RegistrarAuditoria(tentativa.UsuarioID, "SIMULADO_FINALIZADO",
            $"ProvaID: {prova.ProvaID}; Tentativa: {tentativa.ProvaUsuarioID}; Nota: {tentativa.NotaPercentual}" + (tempoEsgotado ? "; tempo esgotado" : ""));
        await context.SaveChangesAsync(ct);
        tentativa.XPGanho = await _recompensas.ConcederAsync(tentativa.UsuarioID, OrigemRecompensa.Prova, tentativa.ProvaUsuarioID, xp, null, $"Simulado: {prova.Titulo}", ct);
        await context.SaveChangesAsync(ct);

        // Simulado aprovado conta no progresso da trilha (pode concluir a trilha).
        if (prova.TrilhaID.HasValue) await matriculas.AtualizarProgressoAsync(tentativa.UsuarioID, prova.TrilhaID.Value, ct);
    }

    // ---------------------------------------------------------------- Montagem das respostas

    private async Task<TentativaDto> MontarAsync(ProvaUsuario t, Prova prova, CancellationToken ct)
    {
        var finalizada = t.Status == StatusTentativa.Finalizada;
        var ordem = await (from pq in context.ProvaQuestoes.AsNoTracking()
                           join q in context.Questoes.AsNoTracking() on pq.QuestaoID equals q.QuestaoID
                           where pq.ProvaID == t.ProvaID && q.Status == StatusConteudo.Aprovado
                           orderby pq.OrdemQuestao
                           select q).ToListAsync(ct);
        var ids = ordem.Select(q => q.QuestaoID).ToList();
        var alternativas = await context.AlternativasQuestao.AsNoTracking().Where(a => ids.Contains(a.QuestaoID)).OrderBy(a => a.AlternativaID).ToListAsync(ct);
        var respostas = await context.RespostasProvasUsuario.AsNoTracking().Where(r => r.ProvaUsuarioID == t.ProvaUsuarioID).ToDictionaryAsync(r => r.QuestaoID, ct);

        var questoes = ordem.Select((q, i) => new QuestaoTentativaDto(q.QuestaoID, i + 1, q.TextoQuestao, q.TipoQuestao,
            alternativas.Where(a => a.QuestaoID == q.QuestaoID).Select(a => new AlternativaTentativaDto(a.AlternativaID, a.TextoAlternativa)).ToList(),
            respostas.TryGetValue(q.QuestaoID, out var r) ? r.AlternativaSelecionadaID : null)).ToList();

        ResultadoTentativaDto? resultado = null;
        if (finalizada)
        {
            var feedback = ordem.Select((q, i) =>
            {
                var alts = alternativas.Where(a => a.QuestaoID == q.QuestaoID).ToList();
                var correta = alts.First(a => a.EhCorreta);
                respostas.TryGetValue(q.QuestaoID, out var resposta);
                var escolhida = resposta is null ? null : alts.FirstOrDefault(a => a.AlternativaID == resposta.AlternativaSelecionadaID);
                return new FeedbackQuestaoDto(q.QuestaoID, i + 1, q.TextoQuestao, resposta?.AlternativaSelecionadaID, correta.AlternativaID,
                    correta.TextoAlternativa, resposta?.EhCorreta == true, q.Explicacao, escolhida?.Explicacao, correta.Explicacao);
            }).ToList();
            var acertos = feedback.Count(f => f.Correta);
            var emBranco = feedback.Count(f => f.AlternativaSelecionadaID is null);
            resultado = new ResultadoTentativaDto(acertos, feedback.Count - acertos - emBranco, emBranco, t.NotaPercentual,
                t.NotaPercentual >= prova.NotaMinimaAprovacao, t.XPGanho,
                t.PrazoEm.HasValue && t.ConcluidoEm.HasValue && t.ConcluidoEm.Value >= t.PrazoEm.Value, feedback);
        }
        return new TentativaDto(t.ProvaUsuarioID, t.ProvaID, prova.Titulo, t.Status, t.NumeroTentativa, t.IniciadoEm, t.PrazoEm, t.ConcluidoEm,
            finalizada ? null : TempoRestante(t), finalizada ? t.TotalQuestoes : questoes.Count, respostas.Count, questoes, resultado);
    }

    private async Task<(ProvaUsuario, Prova)> CarregarAsync(int usuarioId, int tentativaId, CancellationToken ct)
    {
        // Tentativa de outro usuário responde 404 (não revela que existe).
        var tentativa = await context.ProvasUsuario.FirstOrDefaultAsync(t => t.ProvaUsuarioID == tentativaId && t.UsuarioID == usuarioId, ct)
            ?? throw new ResourceNotFoundException("Tentativa não encontrada.");
        var prova = await context.Provas.AsNoTracking().FirstAsync(p => p.ProvaID == tentativa.ProvaID, ct);
        return (tentativa, prova);
    }

    private Task<List<int>> QuestoesDaProvaAsync(int provaId, CancellationToken ct) =>
        (from pq in context.ProvaQuestoes.AsNoTracking()
         join q in context.Questoes.AsNoTracking() on pq.QuestaoID equals q.QuestaoID
         where pq.ProvaID == provaId && q.Status == StatusConteudo.Aprovado
         orderby pq.OrdemQuestao
         select q.QuestaoID).ToListAsync(ct);

    private static bool Expirou(ProvaUsuario t) => t.PrazoEm.HasValue && DateTime.UtcNow > t.PrazoEm.Value + Tolerancia;

    private static int? TempoRestante(ProvaUsuario t) =>
        t.PrazoEm.HasValue ? Math.Max(0, (int)(t.PrazoEm.Value - DateTime.UtcNow).TotalSeconds) : null;
}
