namespace Ducks.Domain.Entities;

public sealed class ChatMessage
{
    public Guid MessageId { get; set; }
    public Guid ConversationId { get; set; }
    public int UsuarioID { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
