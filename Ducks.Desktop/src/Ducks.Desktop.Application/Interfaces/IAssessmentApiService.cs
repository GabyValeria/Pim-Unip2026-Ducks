using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ducks.Desktop.Core.Models;

namespace Ducks.Desktop.Application.Interfaces
{
    /// <summary>Banco de questões e simulados (api/admin/content/questoes e /simulados).</summary>
    public interface IAssessmentApiService
    {
        Task<PagedResult<QuestaoDto>> GetQuestoesAsync(string? busca, string? status, string? dificuldade, int page, int pageSize, CancellationToken cancellationToken = default);
        Task CreateQuestaoAsync(QuestaoRequestDto questao, CancellationToken cancellationToken = default);
        Task UpdateQuestaoAsync(int id, QuestaoRequestDto questao, CancellationToken cancellationToken = default);
        Task DeleteQuestaoAsync(int id, CancellationToken cancellationToken = default);
        Task SendQuestaoToReviewAsync(int id, CancellationToken cancellationToken = default);

        Task<PagedResult<SimuladoDto>> GetSimuladosAsync(string? busca, string? status, int page, int pageSize, CancellationToken cancellationToken = default);
        Task<SimuladoDto?> GetSimuladoAsync(int id, CancellationToken cancellationToken = default);
        Task CreateSimuladoAsync(SimuladoRequestDto simulado, CancellationToken cancellationToken = default);
        Task UpdateSimuladoAsync(int id, SimuladoRequestDto simulado, CancellationToken cancellationToken = default);
        Task DeleteSimuladoAsync(int id, CancellationToken cancellationToken = default);
        Task<SimuladoDto?> SetQuestoesAsync(int id, IReadOnlyList<int> questaoIds, CancellationToken cancellationToken = default);
        Task<SimuladoDto?> SortearQuestoesAsync(int id, SortearQuestoesRequestDto filtros, CancellationToken cancellationToken = default);
        Task SendSimuladoToReviewAsync(int id, CancellationToken cancellationToken = default);
    }

    public interface IXpApiService
    {
        Task AdjustXpAsync(AdicionarXpRequestDto ajuste, CancellationToken cancellationToken = default);
    }
}
