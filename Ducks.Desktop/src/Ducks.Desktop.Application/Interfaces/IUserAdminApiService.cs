using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ducks.Desktop.Core.Models;

namespace Ducks.Desktop.Application.Interfaces
{
    public interface IUserAdminApiService
    {
        Task<PagedResult<UserDto>> GetUsersAsync(string? search = null, int page = 1, int pageSize = 25, CancellationToken cancellationToken = default);
        Task SetBlockedAsync(int userId, bool block, CancellationToken cancellationToken = default);
    }

    public interface IRoleApiService
    {
        Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default);
        /// <summary>Substitui todos os papéis do usuário pelo papel informado (comportamento da API).</summary>
        Task AssignRoleAsync(int userId, int roleId, CancellationToken cancellationToken = default);
    }
}
