using Ducks.Application.DTOs;
using Ducks.Application.Services;
using Ducks.Domain.Entities;
using Ducks.Domain.Exceptions;
using Ducks.Infrastructure.Persistence;
using Ducks.Infrastructure.Services.Comum;
using Microsoft.EntityFrameworkCore;

namespace Ducks.Infrastructure.Services;

public class BancoQuestoesService(DucksDbContext context) : IBancoQuestoesService
{
    // ================================================================ Questões

    public async Task<ResultadoPaginado<QuestaoDto>> ListarQuestoesAsync(FiltroQuestoesDto filtro, CancellationToken ct = default)
    {
        var query = context.Questoes.AsNoTracking().AsQueryable();
        var status = ExtensoesServico.NormalizarStatus(filtro.Status);
        if (status is not null) query = query.Where(q => q.Status == status);
        if (!string.IsNullOrWhiteSpace(filtro.Dificuldade))
        {
            var d = NormalizarDificuldade(filtro.Dificuldade);
            query = query.Where(q => q.NivelDificuldade == d);
        }
        if (!string.IsNullOrWhiteSpace(filtro.Tipo))
        {
            var t = NormalizarTipo(filtro.Tipo);
            query = query.Where(q => q.TipoQuestao == t);
        }
        if (!string.IsNullOrWhiteSpace(filtro.Tag))
        {
            var tag = "," + filtro.Tag.Trim().ToLowerInvariant() + ",";
            query = query.Where(q => q.Tags != null && q.Tags.Contains(tag));
        }
        if (!string.IsNullOrWhiteSpace(filtro.Assunto)) query = query.Where(q => q.Assunto != null && q.Assunto.Contains(filtro.Assunto.Trim()));
        if (filtro.TrilhaID.HasValue) query = query.Where(q => q.TrilhaID == filtro.TrilhaID);
        if (filtro.ModuloID.HasValue) query = query.Where(q => q.ModuloID == filtro.ModuloID);
        if (filtro.AulaID.HasValue) query = query.Where(q => q.AulaID == filtro.AulaID);
        if (!string.IsNullOrWhiteSpace(filtro.Busca)) query = query.Where(q => q.TextoQuestao.Contains(filtro.Busca.Trim()));

        var ordenada = filtro.OrdenarPor?.ToLowerInvariant() switch
        {
            "atualizadoem" => query.Ordenar(q => q.AtualizadoEm, filtro.Decrescente),
            "dificuldade" => query.Ordenar(q => q.NivelDificuldade, filtro.Decrescente),
            "status" => query.Ordenar(q => q.Status, filtro.Decrescente),
            "id" => query.Ordenar(q => q.QuestaoID, filtro.Decrescente),
            // Padrão: mais recentes primeiro.
            _ => query.Ordenar(q => q.CriadoEm, filtro.Decrescente || string.IsNullOrWhiteSpace(filtro.Ordem))
        };
        var pagina = await ordenada.ThenBy(q => q.QuestaoID).PaginarAsync(filtro, ct);
        var alternativas = await CarregarAlternativasAsync(pagina.Items.Select(q => q.QuestaoID).ToList(), ct);
        return new ResultadoPaginado<QuestaoDto>(pagina.Page, pagina.PageSize, pagina.Total,
            pagina.Items.Select(q => ParaDto(q, alternativas.GetValueOrDefault(q.QuestaoID, []))).ToList());
    }

    public async Task<QuestaoDto> ObterQuestaoAsync(int questaoId, CancellationToken ct = default)
    {
        var questao = await context.Questoes.AsNoTracking().FirstOrDefaultAsync(q => q.QuestaoID == questaoId, ct)
            ?? throw new ResourceNotFoundException("Questão não encontrada.");
        var alternativas = await CarregarAlternativasAsync([questaoId], ct);
        return ParaDto(questao, alternativas.GetValueOrDefault(questaoId, []));
    }

    public async Task<QuestaoDto> CriarQuestaoAsync(QuestaoRequestDto dto, int usuarioId, CancellationToken ct = default)
    {
        var (tipo, dificuldade) = ValidarQuestao(dto);
        var vinculo = await ResolverVinculoAsync(dto.TrilhaID, dto.ModuloID, dto.AulaID, dto.MateriaID, exigirVinculo: true, ct);
        var agora = DateTime.UtcNow;
        var questao = new Questao
        {
            TextoQuestao = dto.Enunciado.Trim(), TipoQuestao = tipo, NivelDificuldade = dificuldade, Peso = dto.Peso,
            Explicacao = Limpo(dto.Explicacao), Assunto = Limpo(dto.Assunto), Tags = NormalizarTags(dto.Tags),
            TrilhaID = vinculo.TrilhaID, ModuloID = vinculo.ModuloID, AulaID = vinculo.AulaID, MateriaID = vinculo.MateriaID,
            Status = StatusConteudo.Rascunho, CriadoPorUsuarioID = usuarioId, CriadoEm = agora, AtualizadoEm = agora
        };
        context.Questoes.Add(questao);
        await context.SaveChangesAsync(ct);
        context.AlternativasQuestao.AddRange(dto.Alternativas.Select(a => NovaAlternativa(questao.QuestaoID, a)));
        context.RegistrarAuditoria(usuarioId, "QUESTAO_CRIADA", $"QuestaoID: {questao.QuestaoID}");
        await context.SaveChangesAsync(ct);
        return await ObterQuestaoAsync(questao.QuestaoID, ct);
    }

    public async Task<QuestaoDto> AtualizarQuestaoAsync(int questaoId, QuestaoRequestDto dto, int usuarioId, CancellationToken ct = default)
    {
        var questao = await context.Questoes.FirstOrDefaultAsync(q => q.QuestaoID == questaoId, ct)
            ?? throw new ResourceNotFoundException("Questão não encontrada.");
        var (tipo, dificuldade) = ValidarQuestao(dto);
        var vinculo = await ResolverVinculoAsync(dto.TrilhaID, dto.ModuloID, dto.AulaID, dto.MateriaID ?? questao.MateriaID, exigirVinculo: true, ct);
        questao.Status = ExtensoesServico.StatusAposEdicao(questao.Status);

        var atuais = await context.AlternativasQuestao.Where(a => a.QuestaoID == questaoId).ToListAsync(ct);
        var jaRespondida = await context.RespostasProvasUsuario.AnyAsync(r => r.QuestaoID == questaoId, ct);
        if (jaRespondida)
        {
            // Questão com respostas: o gabarito não pode mudar (o histórico dos estudantes depende dele).
            var mesmoGabarito = dto.Alternativas.Count == atuais.Count
                && dto.Alternativas.All(n => n.AlternativaID.HasValue && atuais.Any(a => a.AlternativaID == n.AlternativaID && a.EhCorreta == n.Correta));
            if (!mesmoGabarito)
                throw new ConflictException("A questão já foi respondida: só é possível corrigir textos e explicações (mantenha as mesmas alternativas e o mesmo gabarito). Para mudar o gabarito, crie uma nova questão.");
            foreach (var nova in dto.Alternativas)
            {
                var alt = atuais.Single(a => a.AlternativaID == nova.AlternativaID);
                alt.TextoAlternativa = nova.Texto.Trim();
                alt.Explicacao = Limpo(nova.Explicacao);
            }
        }
        else
        {
            context.AlternativasQuestao.RemoveRange(atuais);
            context.AlternativasQuestao.AddRange(dto.Alternativas.Select(a => NovaAlternativa(questaoId, a)));
        }
        questao.TextoQuestao = dto.Enunciado.Trim();
        questao.TipoQuestao = tipo;
        questao.NivelDificuldade = dificuldade;
        questao.Peso = dto.Peso;
        questao.Explicacao = Limpo(dto.Explicacao);
        questao.Assunto = Limpo(dto.Assunto);
        questao.Tags = NormalizarTags(dto.Tags);
        questao.TrilhaID = vinculo.TrilhaID;
        questao.ModuloID = vinculo.ModuloID;
        questao.AulaID = vinculo.AulaID;
        questao.MateriaID = vinculo.MateriaID;
        questao.AtualizadoEm = DateTime.UtcNow;
        context.RegistrarAuditoria(usuarioId, "QUESTAO_ATUALIZADA", $"QuestaoID: {questaoId}");
        await context.SaveChangesAsync(ct);
        return await ObterQuestaoAsync(questaoId, ct);
    }

    public async Task ExcluirQuestaoAsync(int questaoId, int usuarioId, CancellationToken ct = default)
    {
        var questao = await context.Questoes.FirstOrDefaultAsync(q => q.QuestaoID == questaoId, ct)
            ?? throw new ResourceNotFoundException("Questão não encontrada.");
        if (await context.RespostasProvasUsuario.AnyAsync(r => r.QuestaoID == questaoId, ct))
            throw new ConflictException("A questão já foi respondida por estudantes e não pode ser excluída. Peça à moderação para removê-la.");
        var vinculos = await context.ProvaQuestoes.Where(pq => pq.QuestaoID == questaoId).ToListAsync(ct);
        var provaIds = vinculos.Select(v => v.ProvaID).Distinct().ToList();
        context.ProvaQuestoes.RemoveRange(vinculos);
        context.AlternativasQuestao.RemoveRange(await context.AlternativasQuestao.Where(a => a.QuestaoID == questaoId).ToListAsync(ct));
        context.Questoes.Remove(questao);
        context.RegistrarAuditoria(usuarioId, "QUESTAO_EXCLUIDA", $"QuestaoID: {questaoId}");
        await context.SaveChangesAsync(ct);
        foreach (var provaId in provaIds) await AtualizarTotalQuestoesAsync(provaId, ct);
        await context.SaveChangesAsync(ct);
    }

    // ================================================================ Simulados (gestão)

    public async Task<ResultadoPaginado<SimuladoDto>> ListarSimuladosAsync(FiltroSimuladosDto filtro, CancellationToken ct = default)
    {
        var query = context.Provas.AsNoTracking().AsQueryable();
        var status = ExtensoesServico.NormalizarStatus(filtro.Status);
        if (status is not null) query = query.Where(p => p.Status == status);
        if (filtro.TrilhaID.HasValue) query = query.Where(p => p.TrilhaID == filtro.TrilhaID);
        if (filtro.ModuloID.HasValue) query = query.Where(p => p.ModuloID == filtro.ModuloID);
        if (!string.IsNullOrWhiteSpace(filtro.Busca)) query = query.Where(p => p.Titulo.Contains(filtro.Busca.Trim()));
        var ordenada = OrdenarSimulados(query, filtro);
        var pagina = await ordenada.PaginarAsync(filtro, ct);
        var itens = new List<SimuladoDto>();
        foreach (var p in pagina.Items) itens.Add(await MontarSimuladoAsync(p, incluirQuestoes: false, ct));
        return new ResultadoPaginado<SimuladoDto>(pagina.Page, pagina.PageSize, pagina.Total, itens);
    }

    public async Task<SimuladoDto> ObterSimuladoAsync(int provaId, CancellationToken ct = default)
    {
        var prova = await context.Provas.AsNoTracking().FirstOrDefaultAsync(p => p.ProvaID == provaId, ct)
            ?? throw new ResourceNotFoundException("Simulado não encontrado.");
        return await MontarSimuladoAsync(prova, incluirQuestoes: true, ct);
    }

    public async Task<SimuladoDto> CriarSimuladoAsync(SimuladoRequestDto dto, int usuarioId, CancellationToken ct = default)
    {
        var vinculo = await ResolverVinculoAsync(dto.TrilhaID, dto.ModuloID, null, null, exigirVinculo: false, ct);
        var agora = DateTime.UtcNow;
        var prova = new Prova { Status = StatusConteudo.Rascunho, CriadoPorUsuarioID = usuarioId, CriadoEm = agora };
        Preencher(prova, dto, vinculo);
        context.Provas.Add(prova);
        await context.SaveChangesAsync(ct);
        context.RegistrarAuditoria(usuarioId, "SIMULADO_CRIADO", $"ProvaID: {prova.ProvaID}; {prova.Titulo}");
        await context.SaveChangesAsync(ct);
        return await ObterSimuladoAsync(prova.ProvaID, ct);
    }

    public async Task<SimuladoDto> AtualizarSimuladoAsync(int provaId, SimuladoRequestDto dto, int usuarioId, CancellationToken ct = default)
    {
        var prova = await context.Provas.FirstOrDefaultAsync(p => p.ProvaID == provaId, ct)
            ?? throw new ResourceNotFoundException("Simulado não encontrado.");
        var vinculo = await ResolverVinculoAsync(dto.TrilhaID, dto.ModuloID, null, null, exigirVinculo: false, ct);
        prova.Status = ExtensoesServico.StatusAposEdicao(prova.Status);
        var quantidadeAtual = await context.ProvaQuestoes.CountAsync(pq => pq.ProvaID == provaId, ct);
        Preencher(prova, dto, vinculo);
        if (quantidadeAtual > 0) prova.TotalQuestoes = quantidadeAtual; // com questões definidas, vale a quantidade real
        context.RegistrarAuditoria(usuarioId, "SIMULADO_ATUALIZADO", $"ProvaID: {provaId}");
        await context.SaveChangesAsync(ct);
        return await ObterSimuladoAsync(provaId, ct);
    }

    public async Task ExcluirSimuladoAsync(int provaId, int usuarioId, CancellationToken ct = default)
    {
        var prova = await context.Provas.FirstOrDefaultAsync(p => p.ProvaID == provaId, ct)
            ?? throw new ResourceNotFoundException("Simulado não encontrado.");
        if (await context.ProvasUsuario.AnyAsync(t => t.ProvaID == provaId, ct))
            throw new ConflictException("O simulado já tem tentativas de estudantes e não pode ser excluído. Peça à moderação para removê-lo.");
        context.ProvaQuestoes.RemoveRange(await context.ProvaQuestoes.Where(pq => pq.ProvaID == provaId).ToListAsync(ct));
        context.Provas.Remove(prova);
        context.RegistrarAuditoria(usuarioId, "SIMULADO_EXCLUIDO", $"ProvaID: {provaId}");
        await context.SaveChangesAsync(ct);
    }

    public async Task<SimuladoDto> DefinirQuestoesAsync(int provaId, IReadOnlyList<int> questaoIds, int usuarioId, CancellationToken ct = default)
    {
        if (questaoIds.Distinct().Count() != questaoIds.Count) throw new BusinessRuleException("Há questões repetidas na lista.");
        var validas = await context.Questoes.AsNoTracking()
            .Where(q => questaoIds.Contains(q.QuestaoID) && q.Status != StatusConteudo.Removido && q.Status != StatusConteudo.Rejeitado)
            .Select(q => q.QuestaoID).ToListAsync(ct);
        var invalidas = questaoIds.Except(validas).ToList();
        if (invalidas.Count > 0)
            throw new BusinessRuleException($"Questões inexistentes, rejeitadas ou removidas: {string.Join(", ", invalidas)}.");
        await SubstituirQuestoesAsync(provaId, questaoIds, substituir: true, usuarioId, "SIMULADO_QUESTOES_DEFINIDAS", ct);
        return await ObterSimuladoAsync(provaId, ct);
    }

    public async Task<SimuladoDto> SortearQuestoesAsync(int provaId, SortearQuestoesDto dto, int usuarioId, CancellationToken ct = default)
    {
        var prova = await context.Provas.AsNoTracking().FirstOrDefaultAsync(p => p.ProvaID == provaId, ct)
            ?? throw new ResourceNotFoundException("Simulado não encontrado.");
        var quantidade = dto.Quantidade ?? prova.TotalQuestoes;
        var trilhaId = dto.TrilhaID ?? prova.TrilhaID;
        var moduloId = dto.ModuloID ?? prova.ModuloID;
        var atuais = dto.SubstituirAtuais ? [] : await context.ProvaQuestoes.Where(pq => pq.ProvaID == provaId).Select(pq => pq.QuestaoID).ToListAsync(ct);

        var query = context.Questoes.AsNoTracking().Where(q => q.Status == StatusConteudo.Aprovado && !atuais.Contains(q.QuestaoID));
        if (moduloId.HasValue) query = query.Where(q => q.ModuloID == moduloId);
        else if (trilhaId.HasValue) query = query.Where(q => q.TrilhaID == trilhaId);
        if (!string.IsNullOrWhiteSpace(dto.Dificuldade))
        {
            var d = NormalizarDificuldade(dto.Dificuldade);
            query = query.Where(q => q.NivelDificuldade == d);
        }
        if (!string.IsNullOrWhiteSpace(dto.Tag))
        {
            var tag = "," + dto.Tag.Trim().ToLowerInvariant() + ",";
            query = query.Where(q => q.Tags != null && q.Tags.Contains(tag));
        }
        if (!string.IsNullOrWhiteSpace(dto.Assunto)) query = query.Where(q => q.Assunto != null && q.Assunto.Contains(dto.Assunto.Trim()));

        var disponiveis = await query.CountAsync(ct);
        if (disponiveis < quantidade)
            throw new BusinessRuleException($"Só há {disponiveis} questão(ões) aprovada(s) com esses filtros; foram pedidas {quantidade}.");
        // Guid.NewGuid() vira NEWID() no SQL Server: ordem aleatória feita pelo banco.
        var sorteadas = await query.OrderBy(q => Guid.NewGuid()).Take(quantidade).Select(q => q.QuestaoID).ToListAsync(ct);
        await SubstituirQuestoesAsync(provaId, sorteadas, dto.SubstituirAtuais, usuarioId, "SIMULADO_QUESTOES_SORTEADAS", ct);
        return await ObterSimuladoAsync(provaId, ct);
    }

    // ================================================================ Simulados (estudante)

    public async Task<ResultadoPaginado<SimuladoEstudanteDto>> ListarSimuladosDisponiveisAsync(int usuarioId, FiltroSimuladosDto filtro, CancellationToken ct = default)
    {
        var query = Disponiveis();
        if (filtro.TrilhaID.HasValue) query = query.Where(p => p.TrilhaID == filtro.TrilhaID);
        if (filtro.ModuloID.HasValue) query = query.Where(p => p.ModuloID == filtro.ModuloID);
        if (!string.IsNullOrWhiteSpace(filtro.Busca)) query = query.Where(p => p.Titulo.Contains(filtro.Busca.Trim()));
        var pagina = await OrdenarSimulados(query, filtro).PaginarAsync(filtro, ct);
        var itens = new List<SimuladoEstudanteDto>();
        foreach (var p in pagina.Items) itens.Add(await MontarParaEstudanteAsync(usuarioId, p, ct));
        return new ResultadoPaginado<SimuladoEstudanteDto>(pagina.Page, pagina.PageSize, pagina.Total, itens);
    }

    public async Task<SimuladoEstudanteDto> ObterSimuladoDisponivelAsync(int usuarioId, int provaId, CancellationToken ct = default)
    {
        var prova = await Disponiveis().FirstOrDefaultAsync(p => p.ProvaID == provaId, ct)
            ?? throw new ResourceNotFoundException("Simulado não encontrado.");
        return await MontarParaEstudanteAsync(usuarioId, prova, ct);
    }

    // ================================================================ Auxiliares

    /// <summary>Simulados que o estudante pode ver: aprovados, com trilha e módulo (se houver) também aprovados.</summary>
    private IQueryable<Prova> Disponiveis() =>
        context.Provas.AsNoTracking().Where(p => p.Status == StatusConteudo.Aprovado
            && (p.TrilhaID == null || context.TrilhasAprendizagem.Any(t => t.TrilhaID == p.TrilhaID && t.Status == StatusConteudo.Aprovado))
            && (p.ModuloID == null || context.Modulos.Any(m => m.ModuloID == p.ModuloID && m.Status == StatusConteudo.Aprovado)));

    private static IOrderedQueryable<Prova> OrdenarSimulados(IQueryable<Prova> query, ParametrosPaginacao filtro) =>
        filtro.OrdenarPor?.ToLowerInvariant() switch
        {
            "titulo" => query.Ordenar(p => p.Titulo, filtro.Decrescente).ThenBy(p => p.ProvaID),
            "atualizadoem" => query.Ordenar(p => p.AtualizadoEm, filtro.Decrescente).ThenBy(p => p.ProvaID),
            _ => query.Ordenar(p => p.CriadoEm, filtro.Decrescente || string.IsNullOrWhiteSpace(filtro.Ordem)).ThenBy(p => p.ProvaID)
        };

    private async Task<SimuladoEstudanteDto> MontarParaEstudanteAsync(int usuarioId, Prova p, CancellationToken ct)
    {
        var numero = await (from pq in context.ProvaQuestoes
                            join q in context.Questoes on pq.QuestaoID equals q.QuestaoID
                            where pq.ProvaID == p.ProvaID && q.Status == StatusConteudo.Aprovado
                            select pq.QuestaoID).CountAsync(ct);
        var tentativas = await context.ProvasUsuario.AsNoTracking().Where(t => t.UsuarioID == usuarioId && t.ProvaID == p.ProvaID).ToListAsync(ct);
        var finalizadas = tentativas.Where(t => t.Status == StatusTentativa.Finalizada).ToList();
        var emAndamento = tentativas.FirstOrDefault(t => t.Status == StatusTentativa.EmAndamento);
        int? restantes = p.MaxTentativas.HasValue ? Math.Max(0, p.MaxTentativas.Value - tentativas.Count(t => t.Status != StatusTentativa.EmAndamento)) : null;
        return new SimuladoEstudanteDto(p.ProvaID, p.Titulo, p.Descricao, p.TrilhaID, p.ModuloID, numero, p.TempoLimiteMinutos,
            p.MaxTentativas, p.NotaMinimaAprovacao, p.RecompensaXP, finalizadas.Count, restantes,
            finalizadas.Count == 0 ? null : finalizadas.Max(t => t.NotaPercentual), emAndamento?.ProvaUsuarioID);
    }

    private async Task<SimuladoDto> MontarSimuladoAsync(Prova p, bool incluirQuestoes, CancellationToken ct)
    {
        var questoes = incluirQuestoes
            ? await (from pq in context.ProvaQuestoes.AsNoTracking()
                     join q in context.Questoes.AsNoTracking() on pq.QuestaoID equals q.QuestaoID
                     where pq.ProvaID == p.ProvaID
                     orderby pq.OrdemQuestao
                     select new QuestaoDoSimuladoDto(q.QuestaoID, pq.OrdemQuestao, q.TextoQuestao, q.NivelDificuldade, q.Status)).ToListAsync(ct)
            : [];
        var tentativas = await context.ProvasUsuario.CountAsync(t => t.ProvaID == p.ProvaID, ct);
        return new SimuladoDto(p.ProvaID, p.Titulo, p.Descricao, p.TipoProva, p.TrilhaID, p.ModuloID, p.MateriaID, p.TempoLimiteMinutos,
            p.MaxTentativas, p.NotaMinimaAprovacao, p.RecompensaXP, p.TotalQuestoes, p.Status, tentativas, p.CriadoEm, p.AtualizadoEm, questoes);
    }

    private async Task SubstituirQuestoesAsync(int provaId, IReadOnlyList<int> questaoIds, bool substituir, int usuarioId, string acao, CancellationToken ct)
    {
        var prova = await context.Provas.FirstOrDefaultAsync(p => p.ProvaID == provaId, ct)
            ?? throw new ResourceNotFoundException("Simulado não encontrado.");
        if (await context.ProvasUsuario.AnyAsync(t => t.ProvaID == provaId && t.Status == StatusTentativa.EmAndamento, ct))
            throw new ConflictException("Há estudantes fazendo este simulado agora. Tente alterar as questões mais tarde.");
        prova.Status = ExtensoesServico.StatusAposEdicao(prova.Status);
        prova.AtualizadoEm = DateTime.UtcNow;

        await using var transacao = await context.Database.BeginTransactionAsync(ct);
        var atuais = await context.ProvaQuestoes.Where(pq => pq.ProvaID == provaId).OrderBy(pq => pq.OrdemQuestao).ToListAsync(ct);
        var mantidas = substituir ? [] : atuais.Select(a => a.QuestaoID).ToList();
        // Remove e grava antes de inserir: o banco do grupo tem índice único em (ProvaID, OrdemQuestao).
        context.ProvaQuestoes.RemoveRange(atuais);
        await context.SaveChangesAsync(ct);
        var ordem = 1;
        foreach (var id in mantidas.Concat(questaoIds))
            context.ProvaQuestoes.Add(new ProvaQuestao { ProvaID = provaId, QuestaoID = id, OrdemQuestao = ordem++ });
        prova.TotalQuestoes = Math.Max(1, ordem - 1);
        context.RegistrarAuditoria(usuarioId, acao, $"ProvaID: {provaId}; questões: {ordem - 1}");
        await context.SaveChangesAsync(ct);
        await transacao.CommitAsync(ct);
    }

    private async Task AtualizarTotalQuestoesAsync(int provaId, CancellationToken ct)
    {
        var prova = await context.Provas.FirstOrDefaultAsync(p => p.ProvaID == provaId, ct);
        if (prova is null) return;
        prova.TotalQuestoes = Math.Max(1, await context.ProvaQuestoes.CountAsync(pq => pq.ProvaID == provaId, ct));
    }

    private sealed record Vinculo(int? TrilhaID, int? ModuloID, int? AulaID, int? MateriaID);

    /// <summary>Completa e confere trilha/módulo/aula (a aula define o módulo, o módulo define a trilha).</summary>
    private async Task<Vinculo> ResolverVinculoAsync(int? trilhaId, int? moduloId, int? aulaId, int? materiaId, bool exigirVinculo, CancellationToken ct)
    {
        if (aulaId.HasValue)
        {
            var aula = await context.Aulas.AsNoTracking().FirstOrDefaultAsync(a => a.AulaID == aulaId, ct) ?? throw new BusinessRuleException("Aula informada não existe.");
            if (moduloId.HasValue && moduloId != aula.ModuloID) throw new BusinessRuleException("A aula não pertence ao módulo informado.");
            moduloId = aula.ModuloID;
        }
        if (moduloId.HasValue)
        {
            var modulo = await context.Modulos.AsNoTracking().FirstOrDefaultAsync(m => m.ModuloID == moduloId, ct) ?? throw new BusinessRuleException("Módulo informado não existe.");
            if (trilhaId.HasValue && trilhaId != modulo.TrilhaID) throw new BusinessRuleException("O módulo não pertence à trilha informada.");
            trilhaId = modulo.TrilhaID;
        }
        if (trilhaId.HasValue)
        {
            var trilha = await context.TrilhasAprendizagem.AsNoTracking().FirstOrDefaultAsync(t => t.TrilhaID == trilhaId, ct) ?? throw new BusinessRuleException("Trilha informada não existe.");
            materiaId ??= trilha.MateriaID;
        }
        if (exigirVinculo && trilhaId is null && materiaId is null)
            throw new BusinessRuleException("Vincule a questão a uma trilha, módulo ou aula (ou informe a matéria).");
        return new Vinculo(trilhaId, moduloId, aulaId, materiaId);
    }

    private static void Preencher(Prova prova, SimuladoRequestDto dto, Vinculo vinculo)
    {
        prova.Titulo = dto.Titulo.Trim();
        prova.Descricao = Limpo(dto.Descricao);
        prova.TrilhaID = vinculo.TrilhaID;
        prova.ModuloID = vinculo.ModuloID;
        prova.MateriaID = vinculo.MateriaID;
        // Restrição do banco do grupo: GERAL sem matéria, ESPECIFICA com matéria.
        prova.TipoProva = vinculo.MateriaID.HasValue ? TiposProva.Especifica : TiposProva.Geral;
        prova.TempoLimiteMinutos = dto.TempoLimiteMinutos;
        prova.MaxTentativas = dto.MaxTentativas;
        prova.NotaMinimaAprovacao = dto.NotaMinimaAprovacao;
        prova.RecompensaXP = dto.RecompensaXP;
        prova.TotalQuestoes = dto.NumeroQuestoes;
        prova.AtualizadoEm = DateTime.UtcNow;
    }

    private static (string Tipo, string Dificuldade) ValidarQuestao(QuestaoRequestDto dto)
    {
        var tipo = NormalizarTipo(dto.Tipo);
        var dificuldade = NormalizarDificuldade(dto.Dificuldade);
        if (dto.Alternativas.Count(a => a.Correta) != 1)
            throw new BusinessRuleException("Marque exatamente uma alternativa correta.");
        if (tipo == TiposQuestao.VerdadeiroFalso && dto.Alternativas.Count != 2)
            throw new BusinessRuleException("Questão de verdadeiro ou falso deve ter exatamente 2 alternativas.");
        if (dto.Alternativas.Select(a => a.Texto.Trim().ToLowerInvariant()).Distinct().Count() != dto.Alternativas.Count)
            throw new BusinessRuleException("Há alternativas com o mesmo texto.");
        if (dto.Tags.Any(t => string.IsNullOrWhiteSpace(t) || t.Trim().Length > 40 || t.Contains(',')))
            throw new BusinessRuleException("Cada tag deve ter de 1 a 40 caracteres e não pode conter vírgula.");
        return (tipo, dificuldade);
    }

    private static string NormalizarTipo(string? tipo)
    {
        var valor = tipo?.Trim().ToUpperInvariant() ?? string.Empty;
        return TiposQuestao.Todos.Contains(valor) ? valor : throw new BusinessRuleException($"Tipo de questão inválido. Use: {string.Join(", ", TiposQuestao.Todos)}.");
    }

    private static string NormalizarDificuldade(string? dificuldade)
    {
        var valor = dificuldade?.Trim().ToUpperInvariant() ?? NiveisDificuldade.Medio;
        return NiveisDificuldade.Todos.Contains(valor) ? valor : throw new BusinessRuleException($"Dificuldade inválida. Use: {string.Join(", ", NiveisDificuldade.Todos)}.");
    }

    /// <summary>Tags guardadas como ",tag1,tag2," (minúsculas) para filtrar por tag exata.</summary>
    private static string? NormalizarTags(IEnumerable<string> tags)
    {
        var lista = tags.Select(t => t.Trim().ToLowerInvariant()).Where(t => t.Length > 0).Distinct().ToList();
        return lista.Count == 0 ? null : "," + string.Join(",", lista) + ",";
    }

    private static IReadOnlyList<string> LerTags(string? tags) =>
        string.IsNullOrWhiteSpace(tags) ? [] : tags.Split(',', StringSplitOptions.RemoveEmptyEntries);

    private static string? Limpo(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    private static AlternativaQuestao NovaAlternativa(int questaoId, AlternativaRequestDto a) =>
        new() { QuestaoID = questaoId, TextoAlternativa = a.Texto.Trim(), EhCorreta = a.Correta, Explicacao = Limpo(a.Explicacao) };

    private async Task<Dictionary<int, List<AlternativaDto>>> CarregarAlternativasAsync(List<int> questaoIds, CancellationToken ct) =>
        (await context.AlternativasQuestao.AsNoTracking().Where(a => questaoIds.Contains(a.QuestaoID)).OrderBy(a => a.AlternativaID).ToListAsync(ct))
        .GroupBy(a => a.QuestaoID)
        .ToDictionary(g => g.Key, g => g.Select(a => new AlternativaDto(a.AlternativaID, a.TextoAlternativa, a.EhCorreta, a.Explicacao)).ToList());

    private static QuestaoDto ParaDto(Questao q, IReadOnlyList<AlternativaDto> alternativas) =>
        new(q.QuestaoID, q.TextoQuestao, q.TipoQuestao, q.NivelDificuldade, q.Peso, q.Explicacao, q.Assunto, LerTags(q.Tags),
            q.TrilhaID, q.ModuloID, q.AulaID, q.MateriaID, q.Status, q.CriadoEm, q.AtualizadoEm, alternativas);
}
