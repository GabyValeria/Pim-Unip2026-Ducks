using System.Collections.Generic;
using System.Threading.Tasks;
using Ducks.Desktop.Core.Models;

namespace Ducks.Desktop.Application.Interfaces
{
    public interface IUserAdminApiService
    {
        Task<IEnumerable<UserDto>> GetUsersAsync(string? search = null);
        Task<bool> ToggleBlockUserAsync(int userId, bool block);
        Task<IEnumerable<UserDto>> GetAuthorsAsync();
        Task<bool> ApproveAuthorAsync(int userId);
    }
}