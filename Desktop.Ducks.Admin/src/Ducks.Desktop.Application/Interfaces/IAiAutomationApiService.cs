using System.Threading.Tasks;
using Ducks.Desktop.Core.Models;

namespace Ducks.Desktop.Application.Interfaces
{
    public interface IAiAutomationApiService
    {
        Task<AiResponseDto> ProcessarTarefaAdminAsync(AiPromptRequestDto request);
    }
}