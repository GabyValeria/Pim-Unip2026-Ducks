using System;
using System.Collections.Generic;

namespace Ducks.Desktop.Core.Models
{
    /// <summary>Envelope paginado: { page, pageSize, total, items }.</summary>
    public class PagedResult<T>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public int Total { get; set; }
        public List<T> Items { get; set; } = new();

        public int TotalPages => PageSize <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling(Total / (double)PageSize));
    }
}
