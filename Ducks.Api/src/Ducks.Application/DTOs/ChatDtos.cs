using System.ComponentModel.DataAnnotations;

namespace Ducks.Application.DTOs;

public sealed class ChatMessageRequestDto
{
    public Guid? ConversationId { get; set; }

    [Required, StringLength(4000, MinimumLength = 1)]
    public string Message { get; set; } = string.Empty;

    [StringLength(100)] public string? ModuleId { get; set; }
    [StringLength(100)] public string? LessonId { get; set; }
}

public sealed record ChatHistoryMessageDto(string Role, string Content);

public sealed record ChatHistoryItemDto(
    Guid MessageId,
    string Role,
    string Content,
    DateTime CreatedAt);

public sealed record DuckBotRequest(
    Guid ConversationId,
    string Message,
    string? ModuleId,
    string? LessonId,
    int UserId,
    IReadOnlyList<ChatHistoryMessageDto> History);

public sealed record ChatMessageResponseDto(
    Guid ConversationId,
    Guid MessageId,
    string Answer,
    string Source,
    bool Fallback,
    DateTime CreatedAt);
