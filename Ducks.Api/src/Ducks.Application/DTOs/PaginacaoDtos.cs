namespace Ducks.Application.DTOs;

/// <summary>
/// Parâmetros comuns das listagens: página, tamanho (máx. 100), campo de ordenação e direção.
/// Segue o mesmo formato de GET api/admin/users (page/pageSize).
/// </summary>
public class ParametrosPaginacao
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    /// <summary>Campo usado na ordenação (cada listagem documenta os valores aceitos).</summary>
    public string? OrdenarPor { get; set; }
    /// <summary>"asc" (padrão) ou "desc".</summary>
    public string? Ordem { get; set; }

    public int PaginaNormalizada => Math.Max(Page, 1);
    public int TamanhoNormalizado => Math.Clamp(PageSize, 1, 100);
    public bool Decrescente => string.Equals(Ordem, "desc", StringComparison.OrdinalIgnoreCase);
}

/// <summary>Resposta paginada no mesmo formato já usado pela API: { page, pageSize, total, items }.</summary>
public sealed record ResultadoPaginado<T>(int Page, int PageSize, int Total, IReadOnlyList<T> Items);
