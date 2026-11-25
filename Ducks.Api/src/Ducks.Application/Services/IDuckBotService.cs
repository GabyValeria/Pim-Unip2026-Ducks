using Ducks.Application.DTOs;

namespace Ducks.Application.Services;

public interface IDuckBotService
{
    Task<ChatMessageResponseDto> SendMessageAsync(DuckBotRequest request, CancellationToken cancellationToken = default);
}

public interface IFaqService
{
    Task<string> SearchAsync(string question, CancellationToken cancellationToken = default);
}
