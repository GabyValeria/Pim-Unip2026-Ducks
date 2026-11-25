using Ducks.Application.DTOs;
using Ducks.Domain.Entities;
using Ducks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ducks.Infrastructure.Services.Comum;

internal static class ExtensoesServico
{
    /// <summary>Lê um número da configuração (sem depender do pacote Configuration.Binder).</summary>
    public static int LerInt(this Microsoft.Extensions.Configuration.IConfiguration configuration, string chave, int padrao) =>
        int.TryParse(configuration[chave], out var valor) ? valor : padrao;

    public static bool LerBool(this Microsoft.Extensions.Configuration.IConfiguration configuration, string chave, bool padrao) =>
        bool.TryParse(configuration[chave], out var valor) ? valor : padrao;

    /// <summary>Acrescenta um registro na auditoria já existente (é gravado no próximo SaveChanges).</summary>
    public static void RegistrarAuditoria(this DucksDbContext context, int usuarioId, string acao, string detalhes) =>
        context.LogsAuditoria.Add(new LogAuditoria
        {
            UsuarioID = usuarioId,
            Acao = acao,
            Detalhes = detalhes.Length > 4000 ? detalhes[..4000] : detalhes,
            CriadoEm = DateTime.UtcNow
        });

    /// <summary>Aplica página e tamanho e devolve no formato { page, pageSize, total, items }.</summary>
    public static async Task<ResultadoPaginado<T>> PaginarAsync<T>(
        this IQueryable<T> query, ParametrosPaginacao parametros, CancellationToken ct)
    {
        var page = parametros.PaginaNormalizada;
        var pageSize = parametros.TamanhoNormalizado;
        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new ResultadoPaginado<T>(page, pageSize, total, items);
    }

    public static IOrderedQueryable<T> Ordenar<T, TKey>(
        this IQueryable<T> query, System.Linq.Expressions.Expression<Func<T, TKey>> chave, bool decrescente) =>
        decrescente ? query.OrderByDescending(chave) : query.OrderBy(chave);

    /// <summary>Valida o filtro de estado; devolve null quando não informado.</summary>
    public static string? NormalizarStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return null;
        var valor = status.Trim().ToUpperInvariant();
        if (valor == "EM_REVISAO") valor = StatusConteudo.EmRevisao;
        if (!StatusConteudo.Todos.Contains(valor))
            throw new Ducks.Domain.Exceptions.BusinessRuleException(
                $"Status inválido. Use: {string.Join(", ", StatusConteudo.Todos)}.");
        return valor;
    }

    /// <summary>
    /// Regra comum de edição: item removido pela moderação não pode ser editado; qualquer outro
    /// item editado volta para RASCUNHO e precisa ser enviado para revisão de novo.
    /// </summary>
    public static string StatusAposEdicao(string statusAtual)
    {
        if (statusAtual == StatusConteudo.Removido)
            throw new Ducks.Domain.Exceptions.ConflictException("Este conteúdo foi removido pela moderação e não pode ser editado.");
        return StatusConteudo.Rascunho;
    }
}
