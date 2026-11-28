using System.Linq;
using Ducks.Desktop.Application.Http;
using Ducks.Desktop.Application.Interfaces;
using Ducks.Desktop.Core.Models;

namespace Ducks.Desktop.Application.Services
{
    public class ContentManagementApiService : IContentManagementApiService
    {
        private readonly IApiClient _api;
        public ContentManagementApiService(IApiClient api) => _api = api;

        public Task UpdateTrilhaAsync(int trilhaId, CreateTrilhaRequestDto trilha, CancellationToken cancellationToken = default) =>
            _api.PutAsync(ApiRoutes.ContentTrilha(trilhaId), trilha, cancellationToken);

        public async Task<IReadOnlyList<ModuloDto>> GetModulosAsync(int trilhaId, CancellationToken cancellationToken = default)
        {
            var page = await _api.GetAsync<PagedResult<ModuloDto>>(ApiRoutes.ContentModulos(trilhaId), cancellationToken);
            return page?.Items ?? new List<ModuloDto>();
        }

        public Task CreateModuloAsync(int trilhaId, ModuloRequestDto modulo, CancellationToken cancellationToken = default) =>
            _api.PostAsync(ApiRoutes.ContentModulosCriar(trilhaId), modulo, cancellationToken);

        public Task UpdateModuloAsync(int moduloId, ModuloRequestDto modulo, CancellationToken cancellationToken = default) =>
            _api.PutAsync(ApiRoutes.ContentModulo(moduloId), modulo, cancellationToken);

        public Task DeleteModuloAsync(int moduloId, CancellationToken cancellationToken = default) =>
            _api.DeleteAsync(ApiRoutes.ContentModulo(moduloId), cancellationToken);

        public Task ReorderModulosAsync(int trilhaId, IReadOnlyList<int> idsNaNovaOrdem, CancellationToken cancellationToken = default) =>
            _api.PutAsync(ApiRoutes.ContentModulosOrdem(trilhaId), new ReordenarRequestDto { Ids = idsNaNovaOrdem.ToList() }, cancellationToken);

        public Task SendModuloToReviewAsync(int moduloId, CancellationToken cancellationToken = default) =>
            _api.PostAsync(ApiRoutes.ContentModuloEnviarRevisao(moduloId), null, cancellationToken);

        public async Task<IReadOnlyList<AulaDto>> GetAulasAsync(int moduloId, CancellationToken cancellationToken = default)
        {
            var page = await _api.GetAsync<PagedResult<AulaDto>>(ApiRoutes.ContentAulas(moduloId), cancellationToken);
            return page?.Items ?? new List<AulaDto>();
        }

        public Task CreateAulaAsync(int moduloId, AulaRequestDto aula, CancellationToken cancellationToken = default) =>
            _api.PostAsync(ApiRoutes.ContentAulasCriar(moduloId), aula, cancellationToken);

        public Task UpdateAulaAsync(int aulaId, AulaRequestDto aula, CancellationToken cancellationToken = default) =>
            _api.PutAsync(ApiRoutes.ContentAula(aulaId), aula, cancellationToken);

        public Task DeleteAulaAsync(int aulaId, CancellationToken cancellationToken = default) =>
            _api.DeleteAsync(ApiRoutes.ContentAula(aulaId), cancellationToken);

        public Task ReorderAulasAsync(int moduloId, IReadOnlyList<int> idsNaNovaOrdem, CancellationToken cancellationToken = default) =>
            _api.PutAsync(ApiRoutes.ContentAulasOrdem(moduloId), new ReordenarRequestDto { Ids = idsNaNovaOrdem.ToList() }, cancellationToken);

        public Task SendAulaToReviewAsync(int aulaId, CancellationToken cancellationToken = default) =>
            _api.PostAsync(ApiRoutes.ContentAulaEnviarRevisao(aulaId), null, cancellationToken);
    }

    public class AssessmentApiService : IAssessmentApiService
    {
        private readonly IApiClient _api;
        public AssessmentApiService(IApiClient api) => _api = api;

        public async Task<PagedResult<QuestaoDto>> GetQuestoesAsync(string? busca, string? status, string? dificuldade, int page, int pageSize, CancellationToken cancellationToken = default) =>
            await _api.GetAsync<PagedResult<QuestaoDto>>(ApiRoutes.ContentQuestoesPage(busca, status, dificuldade, page, pageSize), cancellationToken)
            ?? new PagedResult<QuestaoDto> { Page = page, PageSize = pageSize };

        public Task CreateQuestaoAsync(QuestaoRequestDto questao, CancellationToken cancellationToken = default) =>
            _api.PostAsync(ApiRoutes.ContentQuestoes, questao, cancellationToken);

        public Task UpdateQuestaoAsync(int id, QuestaoRequestDto questao, CancellationToken cancellationToken = default) =>
            _api.PutAsync(ApiRoutes.ContentQuestao(id), questao, cancellationToken);

        public Task DeleteQuestaoAsync(int id, CancellationToken cancellationToken = default) =>
            _api.DeleteAsync(ApiRoutes.ContentQuestao(id), cancellationToken);

        public Task SendQuestaoToReviewAsync(int id, CancellationToken cancellationToken = default) =>
            _api.PostAsync(ApiRoutes.ContentQuestaoEnviarRevisao(id), null, cancellationToken);

        public async Task<PagedResult<SimuladoDto>> GetSimuladosAsync(string? busca, string? status, int page, int pageSize, CancellationToken cancellationToken = default) =>
            await _api.GetAsync<PagedResult<SimuladoDto>>(ApiRoutes.ContentSimuladosPage(busca, status, page, pageSize), cancellationToken)
            ?? new PagedResult<SimuladoDto> { Page = page, PageSize = pageSize };

        public Task<SimuladoDto?> GetSimuladoAsync(int id, CancellationToken cancellationToken = default) =>
            _api.GetAsync<SimuladoDto>(ApiRoutes.ContentSimulado(id), cancellationToken);

        public Task CreateSimuladoAsync(SimuladoRequestDto simulado, CancellationToken cancellationToken = default) =>
            _api.PostAsync(ApiRoutes.ContentSimulados, simulado, cancellationToken);

        public Task UpdateSimuladoAsync(int id, SimuladoRequestDto simulado, CancellationToken cancellationToken = default) =>
            _api.PutAsync(ApiRoutes.ContentSimulado(id), simulado, cancellationToken);

        public Task DeleteSimuladoAsync(int id, CancellationToken cancellationToken = default) =>
            _api.DeleteAsync(ApiRoutes.ContentSimulado(id), cancellationToken);

        public Task<SimuladoDto?> SetQuestoesAsync(int id, IReadOnlyList<int> questaoIds, CancellationToken cancellationToken = default) =>
            _api.PutAsync<SimuladoDto>(ApiRoutes.ContentSimuladoQuestoes(id), new DefinirQuestoesRequestDto { QuestaoIds = questaoIds.ToList() }, cancellationToken);

        public Task<SimuladoDto?> SortearQuestoesAsync(int id, SortearQuestoesRequestDto filtros, CancellationToken cancellationToken = default) =>
            _api.PostAsync<SimuladoDto>(ApiRoutes.ContentSimuladoSortear(id), filtros, cancellationToken);

        public Task SendSimuladoToReviewAsync(int id, CancellationToken cancellationToken = default) =>
            _api.PostAsync(ApiRoutes.ContentSimuladoEnviarRevisao(id), null, cancellationToken);
    }

    public class XpApiService : IXpApiService
    {
        private readonly IApiClient _api;
        public XpApiService(IApiClient api) => _api = api;

        public Task AdjustXpAsync(AdicionarXpRequestDto ajuste, CancellationToken cancellationToken = default) =>
            _api.PostAsync(ApiRoutes.XpAdicionar, ajuste, cancellationToken);
    }
}
