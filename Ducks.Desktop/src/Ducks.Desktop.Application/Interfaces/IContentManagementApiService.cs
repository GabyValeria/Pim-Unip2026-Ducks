using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ducks.Desktop.Core.Models;

namespace Ducks.Desktop.Application.Interfaces
{
    /// <summary>Edição de trilha, módulos e aulas (api/admin/content).</summary>
    public interface IContentManagementApiService
    {
        Task UpdateTrilhaAsync(int trilhaId, CreateTrilhaRequestDto trilha, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ModuloDto>> GetModulosAsync(int trilhaId, CancellationToken cancellationToken = default);
        Task CreateModuloAsync(int trilhaId, ModuloRequestDto modulo, CancellationToken cancellationToken = default);
        Task UpdateModuloAsync(int moduloId, ModuloRequestDto modulo, CancellationToken cancellationToken = default);
        Task DeleteModuloAsync(int moduloId, CancellationToken cancellationToken = default);
        Task ReorderModulosAsync(int trilhaId, IReadOnlyList<int> idsNaNovaOrdem, CancellationToken cancellationToken = default);
        Task SendModuloToReviewAsync(int moduloId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AulaDto>> GetAulasAsync(int moduloId, CancellationToken cancellationToken = default);
        Task CreateAulaAsync(int moduloId, AulaRequestDto aula, CancellationToken cancellationToken = default);
        Task UpdateAulaAsync(int aulaId, AulaRequestDto aula, CancellationToken cancellationToken = default);
        Task DeleteAulaAsync(int aulaId, CancellationToken cancellationToken = default);
        Task ReorderAulasAsync(int moduloId, IReadOnlyList<int> idsNaNovaOrdem, CancellationToken cancellationToken = default);
        Task SendAulaToReviewAsync(int aulaId, CancellationToken cancellationToken = default);
    }
}
