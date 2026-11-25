using Ducks.Application.DTOs;
using Ducks.Application.Services;
using Ducks.Domain.Entities;
using Ducks.Domain.Exceptions;
using Ducks.Infrastructure.Persistence;
using Ducks.Infrastructure.Services.Comum;
using Microsoft.EntityFrameworkCore;

namespace Ducks.Infrastructure.Services;

/// <summary>
/// Progresso de uma trilha = itens concluídos / itens da trilha, onde os itens são as aulas aprovadas
/// dos módulos aprovados e os simulados aprovados ligados à trilha ou aos módulos dela
/// (simulado conta como concluído quando o estudante atinge a nota mínima).
/// </summary>
public class MatriculaService(DucksDbContext context, IEducacionalService educacional) : IMatriculaService
{
    private readonly Recompensas _recompensas = new(context, educacional);

    public async Task<ResultadoMatriculaDto> MatricularAsync(int usuarioId, int trilhaId, CancellationToken ct = default)
    {
        var trilha = await context.TrilhasAprendizagem.AsNoTracking().FirstOrDefaultAsync(t => t.TrilhaID == trilhaId && t.Status == StatusConteudo.Aprovado, ct)
            ?? throw new ResourceNotFoundException("Trilha não encontrada ou ainda não aprovada.");
        var matricula = await context.UsuarioTrilhas.FirstOrDefaultAsync(m => m.UsuarioID == usuarioId && m.TrilhaID == trilhaId, ct);
        if (matricula is not null && matricula.Status != StatusMatricula.Cancelada)
            return new ResultadoMatriculaDto(false, ParaDto(matricula, trilha.NomeTrilha)); // já matriculado: não duplica

        var criada = matricula is null;
        if (matricula is null)
        {
            matricula = new UsuarioTrilha { UsuarioID = usuarioId, TrilhaID = trilhaId, InscritoEm = DateTime.UtcNow };
            context.UsuarioTrilhas.Add(matricula);
        }
        // Reativação de matrícula cancelada: mantém as aulas já concluídas.
        matricula.Status = matricula.ConcluidoEm.HasValue ? StatusMatricula.Concluida : StatusMatricula.Ativa;
        matricula.CanceladoEm = null;
        context.RegistrarAuditoria(usuarioId, criada ? "MATRICULA_CRIADA" : "MATRICULA_REATIVADA", $"TrilhaID: {trilhaId}");
        await context.SaveChangesAsync(ct);
        await AtualizarProgressoAsync(usuarioId, trilhaId, ct);
        return new ResultadoMatriculaDto(true, ParaDto(matricula, trilha.NomeTrilha));
    }

    public async Task CancelarAsync(int usuarioId, int trilhaId, CancellationToken ct = default)
    {
        var matricula = await context.UsuarioTrilhas.FirstOrDefaultAsync(m => m.UsuarioID == usuarioId && m.TrilhaID == trilhaId, ct);
        if (matricula is null || matricula.Status == StatusMatricula.Cancelada)
            throw new ResourceNotFoundException("Você não tem matrícula ativa nesta trilha.");
        if (matricula.Status == StatusMatricula.Concluida)
            throw new ConflictException("A trilha já foi concluída; a matrícula não pode ser cancelada.");
        matricula.Status = StatusMatricula.Cancelada;
        matricula.CanceladoEm = DateTime.UtcNow;
        context.RegistrarAuditoria(usuarioId, "MATRICULA_CANCELADA", $"TrilhaID: {trilhaId}");
        await context.SaveChangesAsync(ct);
    }

    public async Task<ResultadoPaginado<MatriculaDto>> ListarAsync(int usuarioId, FiltroMatriculasDto filtro, CancellationToken ct = default)
    {
        var query = from m in context.UsuarioTrilhas.AsNoTracking()
                    join t in context.TrilhasAprendizagem.AsNoTracking() on m.TrilhaID equals t.TrilhaID
                    where m.UsuarioID == usuarioId
                    select new { m, t.NomeTrilha };
        if (!string.IsNullOrWhiteSpace(filtro.Status))
        {
            var status = filtro.Status.Trim().ToUpperInvariant();
            if (status == "ATIVA") status = StatusMatricula.Ativa;
            query = query.Where(x => x.m.Status == status);
        }
        query = filtro.OrdenarPor?.ToLowerInvariant() switch
        {
            "nome" => filtro.Decrescente ? query.OrderByDescending(x => x.NomeTrilha) : query.OrderBy(x => x.NomeTrilha),
            "progresso" => filtro.Decrescente ? query.OrderByDescending(x => x.m.ProgressoPercentual) : query.OrderBy(x => x.m.ProgressoPercentual),
            // Padrão: matrícula mais recente primeiro.
            _ => filtro.Decrescente || string.IsNullOrWhiteSpace(filtro.Ordem) ? query.OrderByDescending(x => x.m.InscritoEm) : query.OrderBy(x => x.m.InscritoEm)
        };
        return await query.Select(x => new MatriculaDto(x.m.UsuarioTrilhaID, x.m.TrilhaID, x.NomeTrilha, x.m.Status,
                x.m.ProgressoPercentual, x.m.InscritoEm, x.m.ConcluidoEm, x.m.CanceladoEm))
            .PaginarAsync(filtro, ct);
    }

    public async Task<ConclusaoAulaDto> ConcluirAulaAsync(int usuarioId, int aulaId, CancellationToken ct = default)
    {
        var dados = await (from a in context.Aulas.AsNoTracking()
                           join m in context.Modulos.AsNoTracking() on a.ModuloID equals m.ModuloID
                           join t in context.TrilhasAprendizagem.AsNoTracking() on m.TrilhaID equals t.TrilhaID
                           where a.AulaID == aulaId && a.Status == StatusConteudo.Aprovado
                                 && m.Status == StatusConteudo.Aprovado && t.Status == StatusConteudo.Aprovado
                           select new { a.AulaID, a.Titulo, m.ModuloID, t.TrilhaID }).FirstOrDefaultAsync(ct)
            ?? throw new ResourceNotFoundException("Aula não encontrada.");
        var matricula = await context.UsuarioTrilhas.AsNoTracking()
            .FirstOrDefaultAsync(m => m.UsuarioID == usuarioId && m.TrilhaID == dados.TrilhaID && m.Status != StatusMatricula.Cancelada, ct)
            ?? throw new BusinessRuleException("Matricule-se na trilha para registrar a conclusão das aulas.");

        var jaConcluida = await context.ProgressosUsuario.AnyAsync(p => p.UsuarioID == usuarioId && p.AulaID == aulaId, ct);
        var xpAula = 0;
        if (!jaConcluida)
        {
            context.ProgressosUsuario.Add(new ProgressoUsuario { UsuarioID = usuarioId, AulaID = aulaId, ConcluidoEm = DateTime.UtcNow });
            await context.SaveChangesAsync(ct);
            var valor = await _recompensas.LerConfiguracaoAsync(Recompensas.ChaveXPAula, 10, ct);
            xpAula = await _recompensas.ConcederAsync(usuarioId, OrigemRecompensa.Aula, aulaId, valor, null, $"Aula concluída: {dados.Titulo}", ct);
        }
        var atualizacao = await AtualizarProgressoAsync(usuarioId, dados.TrilhaID, ct);
        var calculo = await CalcularAsync(usuarioId, dados.TrilhaID, ct);
        var modulo = calculo.Modulos.FirstOrDefault(m => m.ModuloID == dados.ModuloID);
        return new ConclusaoAulaDto(aulaId, jaConcluida, xpAula, modulo?.Percentual ?? 0, atualizacao.Percentual,
            atualizacao.TrilhaConcluidaAgora, atualizacao.XPConclusaoTrilha);
    }

    public async Task<ProgressoTrilhaDto> ObterProgressoAsync(int usuarioId, int trilhaId, CancellationToken ct = default)
    {
        var matricula = await context.UsuarioTrilhas.AsNoTracking().FirstOrDefaultAsync(m => m.UsuarioID == usuarioId && m.TrilhaID == trilhaId, ct)
            ?? throw new ResourceNotFoundException("Você não está matriculado nesta trilha.");
        var nome = await context.TrilhasAprendizagem.AsNoTracking().Where(t => t.TrilhaID == trilhaId).Select(t => t.NomeTrilha).FirstOrDefaultAsync(ct) ?? string.Empty;
        var calculo = await CalcularAsync(usuarioId, trilhaId, ct);
        return new ProgressoTrilhaDto(trilhaId, nome, matricula.Status, calculo.Percentual, calculo.Concluidos, calculo.Total,
            matricula.ConcluidoEm, calculo.Modulos, calculo.SimuladosDaTrilha);
    }

    public async Task<AtualizacaoProgressoDto> AtualizarProgressoAsync(int usuarioId, int trilhaId, CancellationToken ct = default)
    {
        var matricula = await context.UsuarioTrilhas.FirstOrDefaultAsync(m => m.UsuarioID == usuarioId && m.TrilhaID == trilhaId, ct);
        if (matricula is null || matricula.Status == StatusMatricula.Cancelada) return new AtualizacaoProgressoDto(0, false, 0);
        var calculo = await CalcularAsync(usuarioId, trilhaId, ct);
        matricula.ProgressoPercentual = calculo.Percentual;
        var concluiuAgora = false;
        var xp = 0;
        if (calculo.Total > 0 && calculo.Concluidos == calculo.Total && matricula.Status == StatusMatricula.Ativa)
        {
            matricula.Status = StatusMatricula.Concluida;
            matricula.ConcluidoEm = DateTime.UtcNow;
            concluiuAgora = true;
            context.RegistrarAuditoria(usuarioId, "TRILHA_CONCLUIDA", $"TrilhaID: {trilhaId}");
            var nome = await context.TrilhasAprendizagem.AsNoTracking().Where(t => t.TrilhaID == trilhaId).Select(t => t.NomeTrilha).FirstAsync(ct);
            var valor = await _recompensas.LerConfiguracaoAsync(Recompensas.ChaveXPTrilha, 100, ct);
            xp = await _recompensas.ConcederAsync(usuarioId, OrigemRecompensa.Trilha, trilhaId, valor, null, $"Trilha concluída: {nome}", ct);
        }
        await context.SaveChangesAsync(ct);
        return new AtualizacaoProgressoDto(calculo.Percentual, concluiuAgora, xp);
    }

    public async Task<ResultadoPaginado<AtividadeHistoricoDto>> ListarHistoricoAsync(int usuarioId, FiltroHistoricoDto filtro, CancellationToken ct = default)
    {
        var tipo = filtro.Tipo?.Trim().ToUpperInvariant();
        var itens = new List<AtividadeHistoricoDto>();
        const int limite = 1000;
        if (tipo is null or "" or "AULA_CONCLUIDA")
            itens.AddRange(await (from p in context.ProgressosUsuario.AsNoTracking()
                                  join a in context.Aulas.AsNoTracking() on p.AulaID equals a.AulaID
                                  where p.UsuarioID == usuarioId
                                  orderby p.ConcluidoEm descending
                                  select new AtividadeHistoricoDto("AULA_CONCLUIDA", a.AulaID, a.Titulo, p.ConcluidoEm, null)).Take(limite).ToListAsync(ct));
        if (tipo is null or "" or "TRILHA_CONCLUIDA")
            itens.AddRange(await (from m in context.UsuarioTrilhas.AsNoTracking()
                                  join t in context.TrilhasAprendizagem.AsNoTracking() on m.TrilhaID equals t.TrilhaID
                                  where m.UsuarioID == usuarioId && m.ConcluidoEm != null
                                  select new AtividadeHistoricoDto("TRILHA_CONCLUIDA", t.TrilhaID, t.NomeTrilha, m.ConcluidoEm!.Value, null)).Take(limite).ToListAsync(ct));
        if (tipo is null or "" or "SIMULADO_FINALIZADO")
            itens.AddRange(await (from pu in context.ProvasUsuario.AsNoTracking()
                                  join p in context.Provas.AsNoTracking() on pu.ProvaID equals p.ProvaID
                                  where pu.UsuarioID == usuarioId && pu.Status == StatusTentativa.Finalizada && pu.ConcluidoEm != null
                                  orderby pu.ConcluidoEm descending
                                  select new AtividadeHistoricoDto("SIMULADO_FINALIZADO", pu.ProvaUsuarioID, p.Titulo, pu.ConcluidoEm!.Value, pu.NotaPercentual)).Take(limite).ToListAsync(ct));
        if (tipo is null or "" or "DESAFIO_CONCLUIDO")
            itens.AddRange(await (from pd in context.ParticipacoesDesafio.AsNoTracking()
                                  join d in context.Desafios.AsNoTracking() on pd.DesafioID equals d.DesafioID
                                  where pd.UsuarioID == usuarioId && pd.ConcluidoEm != null
                                  select new AtividadeHistoricoDto("DESAFIO_CONCLUIDO", d.DesafioID, d.Nome, pd.ConcluidoEm!.Value, null)).Take(limite).ToListAsync(ct));

        var filtrados = itens.Where(i => (filtro.De is null || i.Data >= filtro.De) && (filtro.Ate is null || i.Data <= filtro.Ate));
        var ordenados = (string.Equals(filtro.Ordem, "asc", StringComparison.OrdinalIgnoreCase)
            ? filtrados.OrderBy(i => i.Data) : filtrados.OrderByDescending(i => i.Data)).ToList();
        var page = filtro.PaginaNormalizada;
        var size = filtro.TamanhoNormalizado;
        return new ResultadoPaginado<AtividadeHistoricoDto>(page, size, ordenados.Count, ordenados.Skip((page - 1) * size).Take(size).ToList());
    }

    public async Task<DashboardEstudanteDto> ObterDashboardAsync(int usuarioId, CancellationToken ct = default)
    {
        var xp = await context.XPUsuarios.AsNoTracking().FirstOrDefaultAsync(x => x.UsuarioID == usuarioId, ct);
        var matriculas = await ListarAsync(usuarioId, new FiltroMatriculasDto { PageSize = 100 }, ct);
        var ativas = matriculas.Items.Where(m => m.Status == StatusMatricula.Ativa).ToList();
        var tentativas = await context.ProvasUsuario.AsNoTracking()
            .Where(p => p.UsuarioID == usuarioId && p.Status == StatusTentativa.Finalizada)
            .Select(p => p.NotaPercentual).ToListAsync(ct);
        var desafios = await context.ParticipacoesDesafio.AsNoTracking().Where(p => p.UsuarioID == usuarioId).Select(p => p.Status).ToListAsync(ct);
        var conquistas = await (from uc in context.UsuariosConquistas.AsNoTracking()
                                join c in context.Conquistas.AsNoTracking() on uc.ConquistaID equals c.ConquistaID
                                where uc.UsuarioID == usuarioId
                                orderby uc.ConquistadaEm descending
                                select new ConquistaResumoDto(c.ConquistaID, c.Nome, c.Descricao, uc.ConquistadaEm)).ToListAsync(ct);
        var historico = await ListarHistoricoAsync(usuarioId, new FiltroHistoricoDto { PageSize = 5 }, ct);

        return new DashboardEstudanteDto(
            xp?.TotalXP ?? 0, xp?.NivelAtualID ?? 1,
            ativas.Count, matriculas.Items.Count(m => m.Status == StatusMatricula.Concluida),
            await context.ProgressosUsuario.CountAsync(p => p.UsuarioID == usuarioId, ct),
            tentativas.Count, tentativas.Count == 0 ? null : Math.Round(tentativas.Average(), 2),
            desafios.Count(s => s == StatusParticipacao.EmAndamento), desafios.Count(s => s == StatusParticipacao.Concluido),
            conquistas.Count, conquistas.Take(5).ToList(),
            matriculas.Items.Where(m => m.Status != StatusMatricula.Cancelada).Take(10).ToList(),
            historico.Items,
            await RecomendarAsync(usuarioId, ativas, ct));
    }

    // ---------------------------------------------------------------- Cálculo

    private sealed record Calculo(decimal Percentual, int Concluidos, int Total,
        IReadOnlyList<ModuloProgressoDto> Modulos, IReadOnlyList<SimuladoProgressoDto> SimuladosDaTrilha);

    private async Task<Calculo> CalcularAsync(int usuarioId, int trilhaId, CancellationToken ct)
    {
        var modulos = await context.Modulos.AsNoTracking()
            .Where(m => m.TrilhaID == trilhaId && m.Status == StatusConteudo.Aprovado)
            .OrderBy(m => m.IndiceOrdem).ThenBy(m => m.ModuloID).ToListAsync(ct);
        var moduloIds = modulos.Select(m => m.ModuloID).ToList();
        var aulas = await context.Aulas.AsNoTracking()
            .Where(a => moduloIds.Contains(a.ModuloID) && a.Status == StatusConteudo.Aprovado)
            .OrderBy(a => a.IndiceOrdem).ThenBy(a => a.AulaID).ToListAsync(ct);
        var aulaIds = aulas.Select(a => a.AulaID).ToList();
        var concluidas = await context.ProgressosUsuario.AsNoTracking()
            .Where(p => p.UsuarioID == usuarioId && aulaIds.Contains(p.AulaID))
            .ToDictionaryAsync(p => p.AulaID, p => p.ConcluidoEm, ct);
        var simulados = await context.Provas.AsNoTracking()
            .Where(p => p.Status == StatusConteudo.Aprovado && ((p.ModuloID != null && moduloIds.Contains(p.ModuloID.Value)) || (p.ModuloID == null && p.TrilhaID == trilhaId)))
            .ToListAsync(ct);
        var simuladoIds = simulados.Select(s => s.ProvaID).ToList();
        var melhores = await context.ProvasUsuario.AsNoTracking()
            .Where(p => p.UsuarioID == usuarioId && p.Status == StatusTentativa.Finalizada && simuladoIds.Contains(p.ProvaID))
            .GroupBy(p => p.ProvaID).Select(g => new { ProvaID = g.Key, Melhor = g.Max(p => p.NotaPercentual) })
            .ToDictionaryAsync(x => x.ProvaID, x => x.Melhor, ct);

        SimuladoProgressoDto Simulado(Prova p)
        {
            var tem = melhores.TryGetValue(p.ProvaID, out var nota);
            return new SimuladoProgressoDto(p.ProvaID, p.Titulo, tem && nota >= p.NotaMinimaAprovacao, tem ? nota : null);
        }

        var listaModulos = modulos.Select(m =>
        {
            var aulasModulo = aulas.Where(a => a.ModuloID == m.ModuloID)
                .Select(a => new AulaProgressoDto(a.AulaID, a.Titulo, a.IndiceOrdem, concluidas.ContainsKey(a.AulaID),
                    concluidas.TryGetValue(a.AulaID, out var data) ? data : null)).ToList();
            var simuladosModulo = simulados.Where(s => s.ModuloID == m.ModuloID).Select(Simulado).ToList();
            var total = aulasModulo.Count + simuladosModulo.Count;
            var feitos = aulasModulo.Count(a => a.Concluida) + simuladosModulo.Count(s => s.Aprovado);
            return new ModuloProgressoDto(m.ModuloID, m.Titulo, m.IndiceOrdem, Percentual(feitos, total), feitos, total, aulasModulo, simuladosModulo);
        }).ToList();
        var daTrilha = simulados.Where(s => s.ModuloID == null).Select(Simulado).ToList();
        var totalGeral = listaModulos.Sum(m => m.TotalItens) + daTrilha.Count;
        var feitosGeral = listaModulos.Sum(m => m.ItensConcluidos) + daTrilha.Count(s => s.Aprovado);
        return new Calculo(Percentual(feitosGeral, totalGeral), feitosGeral, totalGeral, listaModulos, daTrilha);
    }

    private static decimal Percentual(int feitos, int total) => total == 0 ? 0 : Math.Round(feitos * 100m / total, 2);

    private async Task<IReadOnlyList<RecomendacaoDto>> RecomendarAsync(int usuarioId, IReadOnlyList<MatriculaDto> ativas, CancellationToken ct)
    {
        var recomendacoes = new List<RecomendacaoDto>();
        // 1) Próxima aula de cada trilha em andamento.
        foreach (var matricula in ativas.Take(3))
        {
            var calculo = await CalcularAsync(usuarioId, matricula.TrilhaID, ct);
            var proxima = calculo.Modulos.SelectMany(m => m.Aulas).FirstOrDefault(a => !a.Concluida);
            if (proxima is not null)
                recomendacoes.Add(new RecomendacaoDto("AULA", proxima.AulaID, proxima.Titulo, $"Continue a trilha {matricula.NomeTrilha}."));
            var simulado = calculo.Modulos.SelectMany(m => m.Simulados).Concat(calculo.SimuladosDaTrilha).FirstOrDefault(s => !s.Aprovado);
            if (simulado is not null && proxima is null)
                recomendacoes.Add(new RecomendacaoDto("SIMULADO", simulado.ProvaID, simulado.Titulo, $"Falta este simulado para concluir {matricula.NomeTrilha}."));
        }
        // 2) Desafios vigentes em que o estudante ainda não entrou.
        var agora = DateTime.UtcNow;
        var desafios = await context.Desafios.AsNoTracking()
            .Where(d => d.Ativo && (d.InicioEm == null || d.InicioEm <= agora) && (d.FimEm == null || d.FimEm >= agora)
                        && !context.ParticipacoesDesafio.Any(p => p.DesafioID == d.DesafioID && p.UsuarioID == usuarioId))
            .OrderBy(d => d.FimEm ?? DateTime.MaxValue).Take(2)
            .Select(d => new RecomendacaoDto("DESAFIO", d.DesafioID, d.Nome, $"Vale {d.RecompensaXP} XP.")).ToListAsync(ct);
        recomendacoes.AddRange(desafios);
        // 3) Trilhas aprovadas mais procuradas em que o estudante ainda não está matriculado.
        var trilhas = await context.TrilhasAprendizagem.AsNoTracking()
            .Where(t => t.Status == StatusConteudo.Aprovado && !context.UsuarioTrilhas.Any(m => m.TrilhaID == t.TrilhaID && m.UsuarioID == usuarioId))
            .OrderByDescending(t => context.UsuarioTrilhas.Count(m => m.TrilhaID == t.TrilhaID)).ThenBy(t => t.NomeTrilha).Take(3)
            .Select(t => new RecomendacaoDto("TRILHA", t.TrilhaID, t.NomeTrilha, "Trilha disponível para você começar.")).ToListAsync(ct);
        recomendacoes.AddRange(trilhas);
        return recomendacoes.Take(8).ToList();
    }

    private static MatriculaDto ParaDto(UsuarioTrilha m, string nome) =>
        new(m.UsuarioTrilhaID, m.TrilhaID, nome, m.Status, m.ProgressoPercentual, m.InscritoEm, m.ConcluidoEm, m.CanceladoEm);
}
