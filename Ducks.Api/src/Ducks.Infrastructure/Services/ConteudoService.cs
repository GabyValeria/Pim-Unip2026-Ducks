using Ducks.Application.DTOs;
using Ducks.Application.Services;
using Ducks.Domain.Entities;
using Ducks.Domain.Exceptions;
using Ducks.Infrastructure.Persistence;
using Ducks.Infrastructure.Services.Comum;
using Microsoft.EntityFrameworkCore;

namespace Ducks.Infrastructure.Services;

public class ConteudoService(DucksDbContext context) : IConteudoService
{
    // ---------------------------------------------------------------- Trilhas

    public async Task<TrilhaDetalheDto> ObterTrilhaAsync(int trilhaId, bool somenteAprovado, CancellationToken ct = default)
    {
        var trilha = await context.TrilhasAprendizagem.AsNoTracking().FirstOrDefaultAsync(t => t.TrilhaID == trilhaId, ct);
        if (trilha is null || (somenteAprovado && trilha.Status != StatusConteudo.Aprovado))
            throw new ResourceNotFoundException("Trilha não encontrada.");

        var modulos = await context.Modulos.AsNoTracking()
            .Where(m => m.TrilhaID == trilhaId && (!somenteAprovado || m.Status == StatusConteudo.Aprovado))
            .OrderBy(m => m.IndiceOrdem).ThenBy(m => m.ModuloID)
            .ToListAsync(ct);
        var moduloIds = modulos.Select(m => m.ModuloID).ToList();
        var aulas = await context.Aulas.AsNoTracking()
            .Where(a => moduloIds.Contains(a.ModuloID) && (!somenteAprovado || a.Status == StatusConteudo.Aprovado))
            .OrderBy(a => a.IndiceOrdem).ThenBy(a => a.AulaID)
            .ToListAsync(ct);

        var detalhes = modulos.Select(m => new ModuloDetalheDto(
            m.ModuloID, m.TrilhaID, m.Titulo, m.Descricao, m.IndiceOrdem, m.Status, m.AtualizadoEm,
            aulas.Where(a => a.ModuloID == m.ModuloID).Select(ParaDto).ToList())).ToList();

        return new TrilhaDetalheDto(trilha.TrilhaID, trilha.NomeTrilha, trilha.Descricao, trilha.MateriaID,
            trilha.Status, trilha.AtualizadoEm, detalhes);
    }

    public async Task AtualizarTrilhaAsync(int trilhaId, TrilhaRequestDto dto, int usuarioId, CancellationToken ct = default)
    {
        var trilha = await context.TrilhasAprendizagem.FirstOrDefaultAsync(t => t.TrilhaID == trilhaId, ct)
            ?? throw new ResourceNotFoundException("Trilha não encontrada.");
        trilha.Status = ExtensoesServico.StatusAposEdicao(trilha.Status);
        trilha.NomeTrilha = dto.NomeTrilha.Trim();
        trilha.Descricao = dto.Descricao?.Trim() ?? string.Empty;
        trilha.MateriaID = dto.MateriaID;
        trilha.AtualizadoEm = DateTime.UtcNow;
        context.RegistrarAuditoria(usuarioId, "TRILHA_ATUALIZADA", $"TrilhaID: {trilhaId}");
        await context.SaveChangesAsync(ct);
    }

    // ---------------------------------------------------------------- Módulos

    public async Task<ResultadoPaginado<ModuloDto>> ListarModulosAsync(int trilhaId, FiltroConteudoDto filtro, bool somenteAprovado, CancellationToken ct = default)
    {
        await GarantirTrilhaAsync(trilhaId, somenteAprovado, ct);
        var status = somenteAprovado ? StatusConteudo.Aprovado : ExtensoesServico.NormalizarStatus(filtro.Status);
        var query = context.Modulos.AsNoTracking().Where(m => m.TrilhaID == trilhaId);
        if (status is not null) query = query.Where(m => m.Status == status);
        if (!string.IsNullOrWhiteSpace(filtro.Busca)) query = query.Where(m => m.Titulo.Contains(filtro.Busca.Trim()));

        var ordenada = filtro.OrdenarPor?.ToLowerInvariant() switch
        {
            "titulo" => query.Ordenar(m => m.Titulo, filtro.Decrescente),
            "atualizadoem" => query.Ordenar(m => m.AtualizadoEm, filtro.Decrescente),
            "status" => query.Ordenar(m => m.Status, filtro.Decrescente),
            _ => query.Ordenar(m => m.IndiceOrdem, filtro.Decrescente)
        };
        return await ordenada.ThenBy(m => m.ModuloID)
            .Select(m => new ModuloDto(m.ModuloID, m.TrilhaID, m.Titulo, m.Descricao, m.IndiceOrdem, m.Status,
                context.Aulas.Count(a => a.ModuloID == m.ModuloID && (!somenteAprovado || a.Status == StatusConteudo.Aprovado)),
                m.AtualizadoEm))
            .PaginarAsync(filtro, ct);
    }

    public async Task<ModuloDetalheDto> ObterModuloAsync(int moduloId, bool somenteAprovado, CancellationToken ct = default)
    {
        var modulo = await ObterModuloVisivelAsync(moduloId, somenteAprovado, ct);
        var aulas = await context.Aulas.AsNoTracking()
            .Where(a => a.ModuloID == moduloId && (!somenteAprovado || a.Status == StatusConteudo.Aprovado))
            .OrderBy(a => a.IndiceOrdem).ThenBy(a => a.AulaID).ToListAsync(ct);
        return new ModuloDetalheDto(modulo.ModuloID, modulo.TrilhaID, modulo.Titulo, modulo.Descricao, modulo.IndiceOrdem,
            modulo.Status, modulo.AtualizadoEm, aulas.Select(ParaDto).ToList());
    }

    public async Task<ModuloDto> CriarModuloAsync(int trilhaId, ModuloRequestDto dto, int usuarioId, CancellationToken ct = default)
    {
        var trilha = await context.TrilhasAprendizagem.AsNoTracking().FirstOrDefaultAsync(t => t.TrilhaID == trilhaId, ct)
            ?? throw new ResourceNotFoundException("Trilha não encontrada.");
        if (trilha.Status == StatusConteudo.Removido)
            throw new ConflictException("A trilha foi removida pela moderação.");

        var irmaos = await context.Modulos.Where(m => m.TrilhaID == trilhaId).OrderBy(m => m.IndiceOrdem).ThenBy(m => m.ModuloID).ToListAsync(ct);
        var agora = DateTime.UtcNow;
        var modulo = new Modulo
        {
            TrilhaID = trilhaId,
            Titulo = dto.Titulo.Trim(),
            Descricao = string.IsNullOrWhiteSpace(dto.Descricao) ? null : dto.Descricao.Trim(),
            Status = StatusConteudo.Rascunho,
            CriadoEm = agora,
            AtualizadoEm = agora
        };
        InserirNaPosicao(irmaos, modulo, dto.IndiceOrdem, m => m.IndiceOrdem, (m, i) => m.IndiceOrdem = i);
        context.Modulos.Add(modulo);
        await context.SaveChangesAsync(ct);
        context.RegistrarAuditoria(usuarioId, "MODULO_CRIADO", $"ModuloID: {modulo.ModuloID}; TrilhaID: {trilhaId}; {modulo.Titulo}");
        await context.SaveChangesAsync(ct);
        return new ModuloDto(modulo.ModuloID, modulo.TrilhaID, modulo.Titulo, modulo.Descricao, modulo.IndiceOrdem, modulo.Status, 0, modulo.AtualizadoEm);
    }

    public async Task<ModuloDto> AtualizarModuloAsync(int moduloId, ModuloRequestDto dto, int usuarioId, CancellationToken ct = default)
    {
        var modulo = await context.Modulos.FirstOrDefaultAsync(m => m.ModuloID == moduloId, ct)
            ?? throw new ResourceNotFoundException("Módulo não encontrado.");
        modulo.Status = ExtensoesServico.StatusAposEdicao(modulo.Status);
        modulo.Titulo = dto.Titulo.Trim();
        modulo.Descricao = string.IsNullOrWhiteSpace(dto.Descricao) ? null : dto.Descricao.Trim();
        modulo.AtualizadoEm = DateTime.UtcNow;
        if (dto.IndiceOrdem.HasValue && dto.IndiceOrdem.Value != modulo.IndiceOrdem)
        {
            var irmaos = await context.Modulos.Where(m => m.TrilhaID == modulo.TrilhaID && m.ModuloID != moduloId)
                .OrderBy(m => m.IndiceOrdem).ThenBy(m => m.ModuloID).ToListAsync(ct);
            InserirNaPosicao(irmaos, modulo, dto.IndiceOrdem, m => m.IndiceOrdem, (m, i) => m.IndiceOrdem = i);
        }
        context.RegistrarAuditoria(usuarioId, "MODULO_ATUALIZADO", $"ModuloID: {moduloId}");
        await context.SaveChangesAsync(ct);
        var totalAulas = await context.Aulas.CountAsync(a => a.ModuloID == moduloId, ct);
        return new ModuloDto(modulo.ModuloID, modulo.TrilhaID, modulo.Titulo, modulo.Descricao, modulo.IndiceOrdem, modulo.Status, totalAulas, modulo.AtualizadoEm);
    }

    public async Task ExcluirModuloAsync(int moduloId, int usuarioId, CancellationToken ct = default)
    {
        var modulo = await context.Modulos.FirstOrDefaultAsync(m => m.ModuloID == moduloId, ct)
            ?? throw new ResourceNotFoundException("Módulo não encontrado.");
        var aulaIds = await context.Aulas.Where(a => a.ModuloID == moduloId).Select(a => a.AulaID).ToListAsync(ct);
        await DesvincularAvaliacoesAsync(aulaIds, moduloId, ct);
        context.ProgressosUsuario.RemoveRange(await context.ProgressosUsuario.Where(p => aulaIds.Contains(p.AulaID)).ToListAsync(ct));
        context.Aulas.RemoveRange(await context.Aulas.Where(a => a.ModuloID == moduloId).ToListAsync(ct));
        context.Modulos.Remove(modulo);
        context.RegistrarAuditoria(usuarioId, "MODULO_EXCLUIDO", $"ModuloID: {moduloId}; aulas excluídas: {aulaIds.Count}");
        await context.SaveChangesAsync(ct);
    }

    public async Task ReordenarModulosAsync(int trilhaId, IReadOnlyList<int> moduloIds, int usuarioId, CancellationToken ct = default)
    {
        await GarantirTrilhaAsync(trilhaId, false, ct);
        var modulos = await context.Modulos.Where(m => m.TrilhaID == trilhaId).ToListAsync(ct);
        ValidarListaCompleta(modulos.Select(m => m.ModuloID), moduloIds, "módulos da trilha");
        for (var i = 0; i < moduloIds.Count; i++)
            modulos.Single(m => m.ModuloID == moduloIds[i]).IndiceOrdem = i + 1;
        context.RegistrarAuditoria(usuarioId, "MODULOS_REORDENADOS", $"TrilhaID: {trilhaId}; ordem: {string.Join(",", moduloIds)}");
        await context.SaveChangesAsync(ct);
    }

    // ---------------------------------------------------------------- Aulas

    public async Task<ResultadoPaginado<AulaDto>> ListarAulasAsync(int moduloId, FiltroConteudoDto filtro, bool somenteAprovado, CancellationToken ct = default)
    {
        await ObterModuloVisivelAsync(moduloId, somenteAprovado, ct);
        var status = somenteAprovado ? StatusConteudo.Aprovado : ExtensoesServico.NormalizarStatus(filtro.Status);
        var query = context.Aulas.AsNoTracking().Where(a => a.ModuloID == moduloId);
        if (status is not null) query = query.Where(a => a.Status == status);
        if (!string.IsNullOrWhiteSpace(filtro.Busca)) query = query.Where(a => a.Titulo.Contains(filtro.Busca.Trim()));
        var ordenada = filtro.OrdenarPor?.ToLowerInvariant() switch
        {
            "titulo" => query.Ordenar(a => a.Titulo, filtro.Decrescente),
            "atualizadoem" => query.Ordenar(a => a.AtualizadoEm, filtro.Decrescente),
            "status" => query.Ordenar(a => a.Status, filtro.Decrescente),
            _ => query.Ordenar(a => a.IndiceOrdem, filtro.Decrescente)
        };
        return await ordenada.ThenBy(a => a.AulaID)
            .Select(a => new AulaDto(a.AulaID, a.ModuloID, a.Titulo, a.UrlConteudo, a.Conteudo, a.IndiceOrdem, a.Status, a.AtualizadoEm))
            .PaginarAsync(filtro, ct);
    }

    public async Task<AulaDto> ObterAulaAsync(int aulaId, bool somenteAprovado, CancellationToken ct = default)
    {
        var aula = await context.Aulas.AsNoTracking().FirstOrDefaultAsync(a => a.AulaID == aulaId, ct);
        if (aula is null || (somenteAprovado && aula.Status != StatusConteudo.Aprovado))
            throw new ResourceNotFoundException("Aula não encontrada.");
        if (somenteAprovado) await ObterModuloVisivelAsync(aula.ModuloID, true, ct);
        return ParaDto(aula);
    }

    public async Task<AulaDto> CriarAulaAsync(int moduloId, AulaRequestDto dto, int usuarioId, CancellationToken ct = default)
    {
        var modulo = await context.Modulos.AsNoTracking().FirstOrDefaultAsync(m => m.ModuloID == moduloId, ct)
            ?? throw new ResourceNotFoundException("Módulo não encontrado.");
        if (modulo.Status == StatusConteudo.Removido)
            throw new ConflictException("O módulo foi removido pela moderação.");
        var irmas = await context.Aulas.Where(a => a.ModuloID == moduloId).OrderBy(a => a.IndiceOrdem).ThenBy(a => a.AulaID).ToListAsync(ct);
        var aula = new Aula
        {
            ModuloID = moduloId,
            Titulo = dto.Titulo.Trim(),
            UrlConteudo = dto.UrlConteudo?.Trim() ?? string.Empty,
            Conteudo = string.IsNullOrWhiteSpace(dto.Conteudo) ? null : dto.Conteudo.Trim(),
            Status = StatusConteudo.Rascunho,
            AtualizadoEm = DateTime.UtcNow
        };
        InserirNaPosicao(irmas, aula, dto.IndiceOrdem, a => a.IndiceOrdem, (a, i) => a.IndiceOrdem = i);
        context.Aulas.Add(aula);
        await context.SaveChangesAsync(ct);
        context.RegistrarAuditoria(usuarioId, "AULA_CRIADA", $"AulaID: {aula.AulaID}; ModuloID: {moduloId}; {aula.Titulo}");
        await context.SaveChangesAsync(ct);
        return ParaDto(aula);
    }

    public async Task<AulaDto> AtualizarAulaAsync(int aulaId, AulaRequestDto dto, int usuarioId, CancellationToken ct = default)
    {
        var aula = await context.Aulas.FirstOrDefaultAsync(a => a.AulaID == aulaId, ct)
            ?? throw new ResourceNotFoundException("Aula não encontrada.");
        aula.Status = ExtensoesServico.StatusAposEdicao(aula.Status);
        aula.Titulo = dto.Titulo.Trim();
        aula.UrlConteudo = dto.UrlConteudo?.Trim() ?? string.Empty;
        aula.Conteudo = string.IsNullOrWhiteSpace(dto.Conteudo) ? null : dto.Conteudo.Trim();
        aula.AtualizadoEm = DateTime.UtcNow;
        if (dto.IndiceOrdem.HasValue && dto.IndiceOrdem.Value != aula.IndiceOrdem)
        {
            var irmas = await context.Aulas.Where(a => a.ModuloID == aula.ModuloID && a.AulaID != aulaId)
                .OrderBy(a => a.IndiceOrdem).ThenBy(a => a.AulaID).ToListAsync(ct);
            InserirNaPosicao(irmas, aula, dto.IndiceOrdem, a => a.IndiceOrdem, (a, i) => a.IndiceOrdem = i);
        }
        context.RegistrarAuditoria(usuarioId, "AULA_ATUALIZADA", $"AulaID: {aulaId}");
        await context.SaveChangesAsync(ct);
        return ParaDto(aula);
    }

    public async Task ExcluirAulaAsync(int aulaId, int usuarioId, CancellationToken ct = default)
    {
        var aula = await context.Aulas.FirstOrDefaultAsync(a => a.AulaID == aulaId, ct)
            ?? throw new ResourceNotFoundException("Aula não encontrada.");
        await DesvincularAvaliacoesAsync([aulaId], null, ct);
        context.ProgressosUsuario.RemoveRange(await context.ProgressosUsuario.Where(p => p.AulaID == aulaId).ToListAsync(ct));
        context.Aulas.Remove(aula);
        context.RegistrarAuditoria(usuarioId, "AULA_EXCLUIDA", $"AulaID: {aulaId}; ModuloID: {aula.ModuloID}");
        await context.SaveChangesAsync(ct);
    }

    public async Task ReordenarAulasAsync(int moduloId, IReadOnlyList<int> aulaIds, int usuarioId, CancellationToken ct = default)
    {
        await ObterModuloVisivelAsync(moduloId, false, ct);
        var aulas = await context.Aulas.Where(a => a.ModuloID == moduloId).ToListAsync(ct);
        ValidarListaCompleta(aulas.Select(a => a.AulaID), aulaIds, "aulas do módulo");
        for (var i = 0; i < aulaIds.Count; i++)
            aulas.Single(a => a.AulaID == aulaIds[i]).IndiceOrdem = i + 1;
        context.RegistrarAuditoria(usuarioId, "AULAS_REORDENADAS", $"ModuloID: {moduloId}; ordem: {string.Join(",", aulaIds)}");
        await context.SaveChangesAsync(ct);
    }

    // ---------------------------------------------------------------- Auxiliares

    private async Task GarantirTrilhaAsync(int trilhaId, bool somenteAprovado, CancellationToken ct)
    {
        var existe = await context.TrilhasAprendizagem.AsNoTracking()
            .AnyAsync(t => t.TrilhaID == trilhaId && (!somenteAprovado || t.Status == StatusConteudo.Aprovado), ct);
        if (!existe) throw new ResourceNotFoundException("Trilha não encontrada.");
    }

    private async Task<Modulo> ObterModuloVisivelAsync(int moduloId, bool somenteAprovado, CancellationToken ct)
    {
        var modulo = await context.Modulos.AsNoTracking().FirstOrDefaultAsync(m => m.ModuloID == moduloId, ct);
        if (modulo is null || (somenteAprovado && modulo.Status != StatusConteudo.Aprovado))
            throw new ResourceNotFoundException("Módulo não encontrado.");
        if (somenteAprovado) await GarantirTrilhaAsync(modulo.TrilhaID, true, ct);
        return modulo;
    }

    /// <summary>Questões e simulados ligados à aula/módulo excluído ficam sem esse vínculo (não são apagados).</summary>
    private async Task DesvincularAvaliacoesAsync(IReadOnlyCollection<int> aulaIds, int? moduloId, CancellationToken ct)
    {
        var questoes = await context.Questoes
            .Where(q => (q.AulaID != null && aulaIds.Contains(q.AulaID.Value)) || (moduloId != null && q.ModuloID == moduloId))
            .ToListAsync(ct);
        foreach (var q in questoes)
        {
            if (q.AulaID.HasValue && aulaIds.Contains(q.AulaID.Value)) q.AulaID = null;
            if (moduloId.HasValue && q.ModuloID == moduloId) q.ModuloID = null;
        }
        if (moduloId.HasValue)
            foreach (var p in await context.Provas.Where(p => p.ModuloID == moduloId).ToListAsync(ct))
                p.ModuloID = null;
    }

    /// <summary>Coloca o item na posição pedida (ou no final) e renumera os irmãos de 1 em diante.</summary>
    private static void InserirNaPosicao<T>(List<T> irmaos, T item, int? posicao, Func<T, int> obter, Action<T, int> definir)
    {
        var indice = Math.Clamp((posicao ?? irmaos.Count + 1) - 1, 0, irmaos.Count);
        irmaos.Insert(indice, item);
        for (var i = 0; i < irmaos.Count; i++)
            if (obter(irmaos[i]) != i + 1) definir(irmaos[i], i + 1);
        if (obter(item) != indice + 1) definir(item, indice + 1);
    }

    private static void ValidarListaCompleta(IEnumerable<int> existentes, IReadOnlyList<int> enviados, string descricao)
    {
        var atuais = existentes.ToHashSet();
        if (enviados.Distinct().Count() != enviados.Count || !atuais.SetEquals(enviados))
            throw new BusinessRuleException($"Envie a lista completa de {descricao}, sem repetir IDs.");
    }

    private static AulaDto ParaDto(Aula a) =>
        new(a.AulaID, a.ModuloID, a.Titulo, a.UrlConteudo, a.Conteudo, a.IndiceOrdem, a.Status, a.AtualizadoEm);
}
