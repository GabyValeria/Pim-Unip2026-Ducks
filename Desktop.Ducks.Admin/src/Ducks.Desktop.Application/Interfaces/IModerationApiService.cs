using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ducks.Desktop.Core.Models;

namespace Ducks.Desktop.Application.Interfaces
{
    public interface IModerationApiService
    {
        Task<IEnumerable<ModerationItemDto>> GetPendingQueueAsync();
        Task<bool> ApproveItemAsync(int itemId);
        Task<bool> RejectItemAsync(int itemId);
        Task InitializeSignalRAsync(Action<ModerationItemDto> onNewItemReceived);
        Task StopSignalRAsync();
    }
}