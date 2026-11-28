using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ducks.Desktop.Core.Models;

namespace Ducks.Desktop.Application.Interfaces
{
    public interface IContentApiService
    {
        Task<IReadOnlyList<TrilhaDto>> GetTrilhasAsync(CancellationToken cancellationToken = default);
        Task CreateTrilhaAsync(CreateTrilhaRequestDto trilha, CancellationToken cancellationToken = default);
        Task DeleteTrilhaAsync(int id, CancellationToken cancellationToken = default);
        Task SendTrilhaToReviewAsync(int id, CancellationToken cancellationToken = default);
    }
}
