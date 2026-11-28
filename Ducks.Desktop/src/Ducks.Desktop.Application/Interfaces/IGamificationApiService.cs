using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ducks.Desktop.Core.Models;

namespace Ducks.Desktop.Application.Interfaces
{
    public interface IGamificationApiService
    {
        Task<IReadOnlyList<BadgeDto>> GetBadgesAsync(CancellationToken cancellationToken = default);
        Task CreateBadgeAsync(BadgeRequestDto badge, CancellationToken cancellationToken = default);
        Task UpdateBadgeAsync(int id, BadgeRequestDto badge, CancellationToken cancellationToken = default);
        Task DeleteBadgeAsync(int id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<UserBadgeDto>> GetUserBadgesAsync(int usuarioId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ChallengeDto>> GetChallengesAsync(CancellationToken cancellationToken = default);
        Task CreateChallengeAsync(ChallengeRequestDto challenge, CancellationToken cancellationToken = default);
        Task UpdateChallengeAsync(int id, ChallengeRequestDto challenge, CancellationToken cancellationToken = default);
        Task DeleteChallengeAsync(int id, CancellationToken cancellationToken = default);
    }
}
