using Ducks.Application.DTOs;

namespace Ducks.Application.Services;

public interface IChatConversationService
{
    Task<Guid> GetOrCreateAsync(int usuarioId, Guid? conversationId, CancellationToken cancellationToken = default);
    Task AddMessageAsync(Guid conversationId, int usuarioId, Guid messageId, string role, string content, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChatHistoryMessageDto>> GetRecentMessagesAsync(Guid conversationId, int usuarioId, int take, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChatHistoryItemDto>> GetMessagesAsync(Guid conversationId, int usuarioId, int take, CancellationToken cancellationToken = default);
}
