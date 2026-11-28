using Ducks.Desktop.Application.Http;
using Ducks.Desktop.Application.Interfaces;
using Ducks.Desktop.Core.Models;

namespace Ducks.Desktop.Application.Services
{
    public class UserAdminApiService : IUserAdminApiService
    {
        private readonly IApiClient _api;
        public UserAdminApiService(IApiClient api) => _api = api;

        public async Task<PagedResult<UserDto>> GetUsersAsync(string? search = null, int page = 1, int pageSize = 25, CancellationToken cancellationToken = default)
        {
            var result = await _api.GetAsync<PagedResult<UserDto>>(ApiRoutes.AdminUsersPage(search, page, pageSize), cancellationToken);
            return result ?? new PagedResult<UserDto> { Page = page, PageSize = pageSize };
        }

        public Task SetBlockedAsync(int userId, bool block, CancellationToken cancellationToken = default) =>
            _api.PutAsync(ApiRoutes.AdminUserBlock(userId, block), null, cancellationToken);
    }

    public class RoleApiService : IRoleApiService
    {
        private readonly IApiClient _api;
        public RoleApiService(IApiClient api) => _api = api;

        public async Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default) =>
            await _api.GetAsync<List<RoleDto>>(ApiRoutes.AdminRoles, cancellationToken) ?? new List<RoleDto>();

        public Task AssignRoleAsync(int userId, int roleId, CancellationToken cancellationToken = default) =>
            _api.PostAsync(ApiRoutes.AdminRolesAssign, new AssignRoleRequestDto { UsuarioID = userId, PapelID = roleId }, cancellationToken);
    }
}
