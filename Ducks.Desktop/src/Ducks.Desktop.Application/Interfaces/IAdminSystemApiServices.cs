using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ducks.Desktop.Core.Models;

namespace Ducks.Desktop.Application.Interfaces
{
    public interface IAuditApiService
    {
        Task<IReadOnlyList<AuditLogDto>> GetLogsAsync(CancellationToken cancellationToken = default);
    }

    public interface ISettingsApiService
    {
        Task<IReadOnlyList<SettingDto>> GetSettingsAsync(CancellationToken cancellationToken = default);
        Task SaveSettingAsync(string chave, string valor, CancellationToken cancellationToken = default);
    }

    public interface IAiAutomationApiService
    {
        Task<AiResponseDto> ProcessarTarefaAdminAsync(AiPromptRequestDto request, CancellationToken cancellationToken = default);
    }
}
