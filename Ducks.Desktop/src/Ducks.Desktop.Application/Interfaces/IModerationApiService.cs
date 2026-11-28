using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ducks.Desktop.Core.Models;

namespace Ducks.Desktop.Application.Interfaces
{
    public interface IModerationApiService
    {
        Task<IReadOnlyList<ModerationItemDto>> GetPendingAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ModerationItemDto>> GetHistoryAsync(CancellationToken cancellationToken = default);
        Task ApproveAsync(string tipo, int id, string? comentario = null, CancellationToken cancellationToken = default);
        Task RejectAsync(string tipo, int id, string justificativa, CancellationToken cancellationToken = default);
        Task RemoveAsync(string tipo, int id, string justificativa, CancellationToken cancellationToken = default);

        /// <summary>Atualizações em tempo real (SignalR). Melhor esforço: retorna false se o hub não estiver disponível.</summary>
        Task<bool> TryStartRealtimeAsync(Action<ModerationItemDto> onNewItem);
        Task StopRealtimeAsync();
    }
}
