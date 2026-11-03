using System.Collections.Generic;
using System.Threading.Tasks;
using Ducks.Desktop.Core.Models;

namespace Ducks.Desktop.Application.Interfaces
{
    public interface IGamificationApiService
    {
        Task<IEnumerable<BadgeDto>> GetBadgesAsync();
        Task<bool> CreateBadgeAsync(BadgeDto badge);
        Task<IEnumerable<ChallengeDto>> GetChallengesAsync();
        Task<bool> SaveChallengeAsync(ChallengeDto challenge);
        Task<bool> AdjustUserXpAsync(UserXpAdjustmentDto adjustment);
    }
}