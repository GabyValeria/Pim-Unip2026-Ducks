using Ducks.Desktop.Application.Http;
using Ducks.Desktop.Application.Interfaces;
using Ducks.Desktop.Core.Models;

namespace Ducks.Desktop.Application.Services
{
    public class MetricsApiService : IMetricsApiService
    {
        private readonly IApiClient _api;
        public MetricsApiService(IApiClient api) => _api = api;

        public async Task<DashboardMetricsDto> GetDashboardMetricsAsync(CancellationToken cancellationToken = default) =>
            await _api.GetAsync<DashboardMetricsDto>(ApiRoutes.AdminMetricsSummary, cancellationToken) ?? new DashboardMetricsDto();
    }

    public class AuditApiService : IAuditApiService
    {
        private readonly IApiClient _api;
        public AuditApiService(IApiClient api) => _api = api;

        public async Task<IReadOnlyList<AuditLogDto>> GetLogsAsync(CancellationToken cancellationToken = default) =>
            await _api.GetAsync<List<AuditLogDto>>(ApiRoutes.AdminAuditLogs, cancellationToken) ?? new List<AuditLogDto>();
    }

    public class SettingsApiService : ISettingsApiService
    {
        private readonly IApiClient _api;
        public SettingsApiService(IApiClient api) => _api = api;

        public async Task<IReadOnlyList<SettingDto>> GetSettingsAsync(CancellationToken cancellationToken = default) =>
            await _api.GetAsync<List<SettingDto>>(ApiRoutes.AdminSettings, cancellationToken) ?? new List<SettingDto>();

        public Task SaveSettingAsync(string chave, string valor, CancellationToken cancellationToken = default) =>
            _api.PutAsync(ApiRoutes.AdminSettings, new UpdateSettingRequestDto { Chave = chave, Valor = valor }, cancellationToken);
    }

    public class AiAutomationApiService : IAiAutomationApiService
    {
        private readonly IApiClient _api;
        public AiAutomationApiService(IApiClient api) => _api = api;

        public async Task<AiResponseDto> ProcessarTarefaAdminAsync(AiPromptRequestDto request, CancellationToken cancellationToken = default)
        {
            var result = await _api.PostAsync<AiResponseDto>(ApiRoutes.AdminAiAutomate, request, cancellationToken);
            return result ?? new AiResponseDto { Sucesso = false, MensagemErro = "Resposta vazia da API de IA." };
        }
    }

    public class GamificationApiService : IGamificationApiService
    {
        private readonly IApiClient _api;
        public GamificationApiService(IApiClient api) => _api = api;

        public async Task<IReadOnlyList<BadgeDto>> GetBadgesAsync(CancellationToken cancellationToken = default) =>
            await _api.GetAsync<List<BadgeDto>>(ApiRoutes.GamificationBadges, cancellationToken) ?? new List<BadgeDto>();

        public Task CreateBadgeAsync(BadgeRequestDto badge, CancellationToken cancellationToken = default) =>
            _api.PostAsync(ApiRoutes.GamificationBadges, badge, cancellationToken);

        public Task UpdateBadgeAsync(int id, BadgeRequestDto badge, CancellationToken cancellationToken = default) =>
            _api.PutAsync(ApiRoutes.GamificationBadge(id), badge, cancellationToken);

        public Task DeleteBadgeAsync(int id, CancellationToken cancellationToken = default) =>
            _api.DeleteAsync(ApiRoutes.GamificationBadge(id), cancellationToken);

        public async Task<IReadOnlyList<UserBadgeDto>> GetUserBadgesAsync(int usuarioId, CancellationToken cancellationToken = default) =>
            await _api.GetAsync<List<UserBadgeDto>>(ApiRoutes.GamificationUserBadges(usuarioId), cancellationToken) ?? new List<UserBadgeDto>();

        public async Task<IReadOnlyList<ChallengeDto>> GetChallengesAsync(CancellationToken cancellationToken = default) =>
            await _api.GetAsync<List<ChallengeDto>>(ApiRoutes.GamificationChallenges, cancellationToken) ?? new List<ChallengeDto>();

        public Task CreateChallengeAsync(ChallengeRequestDto challenge, CancellationToken cancellationToken = default) =>
            _api.PostAsync(ApiRoutes.GamificationChallenges, challenge, cancellationToken);

        public Task UpdateChallengeAsync(int id, ChallengeRequestDto challenge, CancellationToken cancellationToken = default) =>
            _api.PutAsync(ApiRoutes.GamificationChallenge(id), challenge, cancellationToken);

        public Task DeleteChallengeAsync(int id, CancellationToken cancellationToken = default) =>
            _api.DeleteAsync(ApiRoutes.GamificationChallenge(id), cancellationToken);
    }

    public class ContentApiService : IContentApiService
    {
        private readonly IApiClient _api;
        public ContentApiService(IApiClient api) => _api = api;

        public async Task<IReadOnlyList<TrilhaDto>> GetTrilhasAsync(CancellationToken cancellationToken = default) =>
            await _api.GetAsync<List<TrilhaDto>>(ApiRoutes.ContentTrilhas, cancellationToken) ?? new List<TrilhaDto>();

        public Task CreateTrilhaAsync(CreateTrilhaRequestDto trilha, CancellationToken cancellationToken = default) =>
            _api.PostAsync(ApiRoutes.ContentTrilhas, trilha, cancellationToken);

        public Task DeleteTrilhaAsync(int id, CancellationToken cancellationToken = default) =>
            _api.DeleteAsync(ApiRoutes.ContentTrilha(id), cancellationToken);

        public Task SendTrilhaToReviewAsync(int id, CancellationToken cancellationToken = default) =>
            _api.PostAsync(ApiRoutes.ContentTrilhaEnviarRevisao(id), null, cancellationToken);
    }
}
