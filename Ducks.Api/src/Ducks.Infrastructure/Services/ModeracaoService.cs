using Ducks.Application.DTOs;
using Ducks.Application.Services;
using Ducks.Domain.Entities;
using Ducks.Domain.Exceptions;
using Ducks.Infrastructure.Persistence;
using Ducks.Infrastructure.Services.Comum;
using Microsoft.EntityFrameworkCore;

namespace Ducks.Infrastructure.Services;

public class ModeracaoService(DucksDbContext context, IConteudoService conteudo) : IModeracaoService
{
    /// <summary>Item carregado para mudança de estado, independente da tabela de origem.</summary>
    private sealed record Alvo(string Tipo, int Id, string Titulo, string Status, Action<string> DefinirStatus);

    public async Task<DecisaoRevisaoDto> EnviarParaRevisaoAsync(string tipo, int conteudoId, int usuarioId, CancellationToken ct = default)
    {
        var alvo = await CarregarAsync(tipo, conteudoId, ct);
        if (alvo.Status == StatusConteudo.EmRevisao) throw new ConflictException("O conteúdo já está em revisão.");
        if (alvo.Status == StatusConteudo.Aprovado) throw new ConflictException("O conteúdo já está aprovado. Edite-o para gerar uma nova versão.");
        if (alvo.Status == StatusConteudo.Removido) throw new ConflictException("O conteúdo foi removido pela moderação.");
        if (!StatusConteudo.PodeEnviarParaRevisao(alvo.Status))
            throw new BusinessRuleException($"Conteúdo com estado {alvo.Status} não pode ser enviado para revisão.");
        await ValidarAntesDeEnviarAsync(alvo, ct);
        return await MudarEstadoAsync(alvo, StatusConteudo.EmRevisao, AcaoRevisao.EnviadoParaRevisao, null, usuarioId, "CONTEUDO_ENVIADO_REVISAO", ct);
    }

    public async Task<DecisaoRevisaoDto> AprovarAsync(string tipo, int conteudoId, int usuarioId, string? comentario, CancellationToken ct = default)
    {
        var alvo = await CarregarAsync(tipo, conteudoId, ct);
        ExigirPendente(alvo);
        if (alvo.Tipo == TipoConteudo.Simulado)
        {
            var questaoIds = await context.ProvaQuestoes.Where(pq => pq.ProvaID == conteudoId).Select(pq => pq.QuestaoID).ToListAsync(ct);
            if (questaoIds.Count == 0) throw new BusinessRuleException("O simulado não tem questões.");
            var naoAprovadas = await context.Questoes.CountAsync(q => questaoIds.Contains(q.QuestaoID) && q.Status != StatusConteudo.Aprovado, ct);
            if (naoAprovadas > 0)
                throw new BusinessRuleException($"Aprove antes as questões do simulado ({naoAprovadas} ainda não aprovada(s)).");
        }
        return await MudarEstadoAsync(alvo, StatusConteudo.Aprovado, AcaoRevisao.Aprovado,
            string.IsNullOrWhiteSpace(comentario) ? null : comentario.Trim(), usuarioId, "CONTEUDO_APROVADO", ct);
    }

    public async Task<DecisaoRevisaoDto> RejeitarAsync(string tipo, int conteudoId, int usuarioId, string justificativa, CancellationToken ct = default)
    {
        var texto = ExigirJustificativa(justificativa);
        var alvo = await CarregarAsync(tipo, conteudoId, ct);
        ExigirPendente(alvo);
        return await MudarEstadoAsync(alvo, StatusConteudo.Rejeitado, AcaoRevisao.Rejeitado, texto, usuarioId, "CONTEUDO_REJEITADO", ct);
    }

    public async Task<DecisaoRevisaoDto> RemoverAsync(string tipo, int conteudoId, int usuarioId, string justificativa, CancellationToken ct = default)
    {
        var texto = ExigirJustificativa(justificativa);
        var alvo = await CarregarAsync(tipo, conteudoId, ct);
        if (alvo.Status == StatusConteudo.Removido) throw new ConflictException("O conteúdo já foi removido.");
        return await MudarEstadoAsync(alvo, StatusConteudo.Removido, AcaoRevisao.Removido, texto, usuarioId, "CONTEUDO_REMOVIDO", ct);
    }

    public async Task<ResultadoPaginado<ItemPendenteDto>> ListarPendentesAsync(FiltroPendentesDto filtro, CancellationToken ct = default)
    {
        string? tipo = null;
        if (!string.IsNullOrWhiteSpace(filtro.Tipo))
            tipo = TipoConteudo.Normalizar(filtro.Tipo) ?? throw new BusinessRuleException($"Tipo inválido. Use: {string.Join(", ", TipoConteudo.Todos)}.");
        var pendente = StatusConteudo.EmRevisao;
        var itens = new List<(string Tipo, int Id, string Titulo, string? Contexto, DateTime AtualizadoEm)>();

        if (tipo is null or TipoConteudo.Trilha)
            itens.AddRange((await context.TrilhasAprendizagem.AsNoTracking().Where(t => t.Status == pendente)
                .Select(t => new { t.TrilhaID, t.NomeTrilha, t.AtualizadoEm }).ToListAsync(ct))
                .Select(t => (TipoConteudo.Trilha, t.TrilhaID, t.NomeTrilha, (string?)null, t.AtualizadoEm)));
        if (tipo is null or TipoConteudo.Modulo)
            itens.AddRange((await (from m in context.Modulos.AsNoTracking()
                                   join t in context.TrilhasAprendizagem.AsNoTracking() on m.TrilhaID equals t.TrilhaID
                                   where m.Status == pendente
                                   select new { m.ModuloID, m.Titulo, t.NomeTrilha, m.AtualizadoEm }).ToListAsync(ct))
                .Select(m => (TipoConteudo.Modulo, m.ModuloID, m.Titulo, (string?)$"Trilha: {m.NomeTrilha}", m.AtualizadoEm)));
        if (tipo is null or TipoConteudo.Aula)
            itens.AddRange((await (from a in context.Aulas.AsNoTracking()
                                   join m in context.Modulos.AsNoTracking() on a.ModuloID equals m.ModuloID
                                   where a.Status == pendente
                                   select new { a.AulaID, a.Titulo, Modulo = m.Titulo, a.AtualizadoEm }).ToListAsync(ct))
                .Select(a => (TipoConteudo.Aula, a.AulaID, a.Titulo, (string?)$"Módulo: {a.Modulo}", a.AtualizadoEm)));
        if (tipo is null or TipoConteudo.Questao)
            itens.AddRange((await context.Questoes.AsNoTracking().Where(q => q.Status == pendente)
                .Select(q => new { q.QuestaoID, q.TextoQuestao, q.Assunto, q.AtualizadoEm }).ToListAsync(ct))
                .Select(q => (TipoConteudo.Questao, q.QuestaoID, Resumir(q.TextoQuestao), q.Assunto, q.AtualizadoEm)));
        if (tipo is null or TipoConteudo.Simulado)
            itens.AddRange((await context.Provas.AsNoTracking().Where(p => p.Status == pendente)
                .Select(p => new { p.ProvaID, p.Titulo, p.AtualizadoEm }).ToListAsync(ct))
                .Select(p => (TipoConteudo.Simulado, p.ProvaID, p.Titulo, (string?)null, p.AtualizadoEm)));

        if (!string.IsNullOrWhiteSpace(filtro.Busca))
            itens = itens.Where(i => i.Titulo.Contains(filtro.Busca.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();

        // Data do último envio para revisão (o mais antigo aparece primeiro: fila de revisão).
        var envios = await context.RevisoesConteudo.AsNoTracking()
            .Where(r => r.Acao == AcaoRevisao.EnviadoParaRevisao)
            .GroupBy(r => new { r.TipoConteudo, r.ConteudoID })
            .Select(g => new { g.Key.TipoConteudo, g.Key.ConteudoID, Ultimo = g.Max(r => r.CriadoEm) })
            .ToListAsync(ct);
        var mapa = envios.ToDictionary(e => (e.TipoConteudo, e.ConteudoID), e => e.Ultimo);
        var lista = itens.Select(i => new ItemPendenteDto(i.Tipo, i.Id, i.Titulo, i.Contexto,
            mapa.TryGetValue((i.Tipo, i.Id), out var envio) ? envio : i.AtualizadoEm));

        lista = filtro.OrdenarPor?.ToLowerInvariant() switch
        {
            "titulo" => filtro.Decrescente ? lista.OrderByDescending(i => i.Titulo) : lista.OrderBy(i => i.Titulo),
            "tipo" => filtro.Decrescente ? lista.OrderByDescending(i => i.Tipo) : lista.OrderBy(i => i.Tipo),
            _ => filtro.Decrescente ? lista.OrderByDescending(i => i.EnviadoEm) : lista.OrderBy(i => i.EnviadoEm)
        };
        var todos = lista.ToList();
        var page = filtro.PaginaNormalizada;
        var size = filtro.TamanhoNormalizado;
        return new ResultadoPaginado<ItemPendenteDto>(page, size, todos.Count, todos.Skip((page - 1) * size).Take(size).ToList());
    }

    public async Task<ItemRevisaoDto> ObterItemAsync(string tipo, int conteudoId, CancellationToken ct = default)
    {
        var alvo = await CarregarAsync(tipo, conteudoId, ct, rastrear: false);
        object detalhes = alvo.Tipo switch
        {
            TipoConteudo.Trilha => await conteudo.ObterTrilhaAsync(conteudoId, somenteAprovado: false, ct),
            TipoConteudo.Modulo => await conteudo.ObterModuloAsync(conteudoId, somenteAprovado: false, ct),
            TipoConteudo.Aula => await conteudo.ObterAulaAsync(conteudoId, somenteAprovado: false, ct),
            TipoConteudo.Questao => await DetalharQuestaoAsync(conteudoId, ct),
            _ => await DetalharSimuladoAsync(conteudoId, ct)
        };
        var historico = await ListarHistoricoAsync(new FiltroHistoricoRevisaoDto { Tipo = alvo.Tipo, ConteudoId = conteudoId, PageSize = 100 }, ct);
        return new ItemRevisaoDto(alvo.Tipo, conteudoId, alvo.Titulo, alvo.Status, detalhes, historico.Items);
    }

    public async Task<ResultadoPaginado<RevisaoDto>> ListarHistoricoAsync(FiltroHistoricoRevisaoDto filtro, CancellationToken ct = default)
    {
        var query = context.RevisoesConteudo.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(filtro.Tipo))
        {
            var tipo = TipoConteudo.Normalizar(filtro.Tipo) ?? throw new BusinessRuleException("Tipo inválido.");
            query = query.Where(r => r.TipoConteudo == tipo);
        }
        if (filtro.ConteudoId.HasValue) query = query.Where(r => r.ConteudoID == filtro.ConteudoId.Value);
        if (!string.IsNullOrWhiteSpace(filtro.Acao))
        {
            var acao = filtro.Acao.Trim().ToUpperInvariant();
            query = query.Where(r => r.Acao == acao);
        }
        // Histórico: mais recente primeiro, a menos que ordem=asc.
        var ordenada = string.Equals(filtro.Ordem, "asc", StringComparison.OrdinalIgnoreCase)
            ? query.OrderBy(r => r.CriadoEm).ThenBy(r => r.RevisaoID)
            : query.OrderByDescending(r => r.CriadoEm).ThenByDescending(r => r.RevisaoID);
        return await (from r in ordenada
                      join u in context.Usuarios.AsNoTracking() on r.UsuarioID equals u.UsuarioID into usuarios
                      from u in usuarios.DefaultIfEmpty()
                      select new RevisaoDto(r.RevisaoID, r.TipoConteudo, r.ConteudoID, r.Acao, r.StatusAnterior, r.StatusNovo,
                          r.Justificativa, r.UsuarioID, u != null ? u.NomeUsuario : null, r.CriadoEm))
            .PaginarAsync(filtro, ct);
    }

    // ---------------------------------------------------------------- Auxiliares

    private async Task<DecisaoRevisaoDto> MudarEstadoAsync(Alvo alvo, string novoStatus, string acao, string? justificativa,
        int usuarioId, string acaoAuditoria, CancellationToken ct)
    {
        var agora = DateTime.UtcNow;
        alvo.DefinirStatus(novoStatus);
        context.RevisoesConteudo.Add(new RevisaoConteudo
        {
            TipoConteudo = alvo.Tipo,
            ConteudoID = alvo.Id,
            Acao = acao,
            StatusAnterior = alvo.Status,
            StatusNovo = novoStatus,
            Justificativa = justificativa,
            UsuarioID = usuarioId,
            CriadoEm = agora
        });
        context.RegistrarAuditoria(usuarioId, acaoAuditoria,
            $"Tipo: {alvo.Tipo}; ID: {alvo.Id}; {alvo.Status} -> {novoStatus}" + (justificativa is null ? "" : $"; Justificativa: {justificativa}"));
        await context.SaveChangesAsync(ct);
        return new DecisaoRevisaoDto(alvo.Tipo, alvo.Id, alvo.Status, novoStatus, agora);
    }

    private async Task<Alvo> CarregarAsync(string tipo, int id, CancellationToken ct, bool rastrear = true)
    {
        var t = TipoConteudo.Normalizar(tipo) ?? throw new BusinessRuleException($"Tipo inválido. Use: {string.Join(", ", TipoConteudo.Todos)}.");
        var agora = DateTime.UtcNow;
        switch (t)
        {
            case TipoConteudo.Trilha:
                var trilha = await context.TrilhasAprendizagem.FirstOrDefaultAsync(x => x.TrilhaID == id, ct) ?? throw new ResourceNotFoundException("Trilha não encontrada.");
                return new Alvo(t, id, trilha.NomeTrilha, trilha.Status, s => { trilha.Status = s; trilha.AtualizadoEm = agora; });
            case TipoConteudo.Modulo:
                var modulo = await context.Modulos.FirstOrDefaultAsync(x => x.ModuloID == id, ct) ?? throw new ResourceNotFoundException("Módulo não encontrado.");
                return new Alvo(t, id, modulo.Titulo, modulo.Status, s => { modulo.Status = s; modulo.AtualizadoEm = agora; });
            case TipoConteudo.Aula:
                var aula = await context.Aulas.FirstOrDefaultAsync(x => x.AulaID == id, ct) ?? throw new ResourceNotFoundException("Aula não encontrada.");
                return new Alvo(t, id, aula.Titulo, aula.Status, s => { aula.Status = s; aula.AtualizadoEm = agora; });
            case TipoConteudo.Questao:
                var questao = await context.Questoes.FirstOrDefaultAsync(x => x.QuestaoID == id, ct) ?? throw new ResourceNotFoundException("Questão não encontrada.");
                return new Alvo(t, id, Resumir(questao.TextoQuestao), questao.Status, s => { questao.Status = s; questao.AtualizadoEm = agora; });
            default:
                var prova = await context.Provas.FirstOrDefaultAsync(x => x.ProvaID == id, ct) ?? throw new ResourceNotFoundException("Simulado não encontrado.");
                return new Alvo(t, id, prova.Titulo, prova.Status, s => { prova.Status = s; prova.AtualizadoEm = agora; });
        }
    }

    private async Task ValidarAntesDeEnviarAsync(Alvo alvo, CancellationToken ct)
    {
        if (alvo.Tipo == TipoConteudo.Simulado && !await context.ProvaQuestoes.AnyAsync(pq => pq.ProvaID == alvo.Id, ct))
            throw new BusinessRuleException("Adicione questões ao simulado antes de enviá-lo para revisão.");
        if (alvo.Tipo == TipoConteudo.Aula)
        {
            var aula = await context.Aulas.AsNoTracking().FirstAsync(a => a.AulaID == alvo.Id, ct);
            if (string.IsNullOrWhiteSpace(aula.UrlConteudo) && string.IsNullOrWhiteSpace(aula.Conteudo))
                throw new BusinessRuleException("Informe o link (UrlConteudo) ou o texto (Conteudo) da aula antes de enviá-la para revisão.");
        }
    }

    private static void ExigirPendente(Alvo alvo)
    {
        if (alvo.Status != StatusConteudo.EmRevisao)
            throw new ConflictException($"Só é possível decidir sobre conteúdo em revisão (estado atual: {alvo.Status}).");
    }

    private static string ExigirJustificativa(string? justificativa)
    {
        var texto = justificativa?.Trim();
        if (string.IsNullOrWhiteSpace(texto) || texto.Length < 5)
            throw new BusinessRuleException("A justificativa é obrigatória (mínimo de 5 caracteres).");
        return texto.Length > 1000 ? texto[..1000] : texto;
    }

    private async Task<object> DetalharQuestaoAsync(int id, CancellationToken ct)
    {
        var q = await context.Questoes.AsNoTracking().FirstAsync(x => x.QuestaoID == id, ct);
        var alternativas = await context.AlternativasQuestao.AsNoTracking().Where(a => a.QuestaoID == id)
            .OrderBy(a => a.AlternativaID)
            .Select(a => new { a.AlternativaID, a.TextoAlternativa, a.EhCorreta, a.Explicacao }).ToListAsync(ct);
        return new
        {
            q.QuestaoID, Enunciado = q.TextoQuestao, q.TipoQuestao, q.NivelDificuldade, q.Peso, q.Explicacao,
            q.Assunto, q.Tags, q.TrilhaID, q.ModuloID, q.AulaID, q.Status, Alternativas = alternativas
        };
    }

    private async Task<object> DetalharSimuladoAsync(int id, CancellationToken ct)
    {
        var p = await context.Provas.AsNoTracking().FirstAsync(x => x.ProvaID == id, ct);
        var questoes = await (from pq in context.ProvaQuestoes.AsNoTracking()
                              join q in context.Questoes.AsNoTracking() on pq.QuestaoID equals q.QuestaoID
                              where pq.ProvaID == id
                              orderby pq.OrdemQuestao
                              select new { q.QuestaoID, pq.OrdemQuestao, Enunciado = q.TextoQuestao, q.NivelDificuldade, q.Status }).ToListAsync(ct);
        return new
        {
            p.ProvaID, p.Titulo, p.Descricao, p.TrilhaID, p.ModuloID, p.TempoLimiteMinutos, p.MaxTentativas,
            p.NotaMinimaAprovacao, p.RecompensaXP, p.Status, Questoes = questoes
        };
    }

    private static string Resumir(string texto) => texto.Length <= 120 ? texto : texto[..117] + "...";
}
