namespace Ducks.Domain.Entities;

public sealed class ChatConversation
{
    public Guid ConversationId { get; set; }
    public int UsuarioID { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }
}
