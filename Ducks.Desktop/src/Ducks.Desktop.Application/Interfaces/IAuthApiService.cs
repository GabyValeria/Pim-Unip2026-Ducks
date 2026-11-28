using System;
using System.Threading;
using System.Threading.Tasks;
using Ducks.Desktop.Core.Models;

namespace Ducks.Desktop.Application.Interfaces
{
    public interface IAuthApiService
    {
        string? CurrentToken { get; set; }
        string? CurrentUserName { get; }
        int CurrentUserId { get; }
        bool IsAuthenticated { get; }

        /// <summary>Disparado quando a sessão não pôde ser renovada (JWT expirado/revogado).</summary>
        event Action? SessionExpired;

        Task<AuthResponseDto?> LoginAsync(string email, string senha);
        Task<bool> TryRefreshAsync(CancellationToken cancellationToken = default);
        void NotifySessionExpired();
        void Logout();
    }
}
