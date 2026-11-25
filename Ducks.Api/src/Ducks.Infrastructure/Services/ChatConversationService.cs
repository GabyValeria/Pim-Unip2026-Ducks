using Ducks.Application.DTOs;
using Ducks.Application.Services;
using Ducks.Domain.Entities;
using Ducks.Domain.Exceptions;
using Ducks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ducks.Infrastructure.Services;

public sealed class ChatConversationService(DucksDbContext context) : IChatConversationService
{
    public async Task<Guid> GetOrCreateAsync(int usuarioId, Guid? conversationId, CancellationToken cancellationToken = default)
    {
        if (usuarioId <= 0) throw new UnauthorizedAccessException();
        var now = DateTime.UtcNow;

        if (conversationId.HasValue)
        {
            var existing = await context.ChatConversations
                .FirstOrDefaultAsync(c => c.ConversationId == conversationId.Value, cancellationToken);
            if (existing is not null)
            {
                if (existing.UsuarioID != usuarioId)
                    throw new ResourceNotFoundException("Conversa não encontrada.");

                existing.AtualizadoEm = now;
                await context.SaveChangesAsync(cancellationToken);
                return existing.ConversationId;
            }
        }

        var id = conversationId ?? Guid.NewGuid();
        context.ChatConversations.Add(new ChatConversation
        {
            ConversationId = id,
            UsuarioID = usuarioId,
            CriadoEm = now,
            AtualizadoEm = now
        });
        await context.SaveChangesAsync(cancellationToken);
        return id;
    }

    public async Task AddMessageAsync(
        Guid conversationId,
        int usuarioId,
        Guid messageId,
        string role,
        string content,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("A mensagem não pode ser vazia.", nameof(content));
        if (content.Length > 12000)
            throw new ArgumentException("A mensagem excede o limite permitido.", nameof(content));
        if (role is not ("user" or "assistant"))
            throw new ArgumentException("Papel de mensagem inválido.", nameof(role));

        var conversation = await context.ChatConversations
            .FirstOrDefaultAsync(c => c.ConversationId == conversationId && c.UsuarioID == usuarioId, cancellationToken);
        if (conversation is null)
            throw new ResourceNotFoundException("Conversa não encontrada.");

        context.ChatMessages.Add(new ChatMessage
        {
            MessageId = messageId == Guid.Empty ? Guid.NewGuid() : messageId,
            ConversationId = conversationId,
            UsuarioID = usuarioId,
            Role = role,
            Content = content,
            CreatedAt = DateTime.UtcNow
        });
        conversation.AtualizadoEm = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ChatHistoryMessageDto>> GetRecentMessagesAsync(
        Guid conversationId, int usuarioId, int take, CancellationToken cancellationToken = default)
    {
        var ownsConversation = await context.ChatConversations.AsNoTracking()
            .AnyAsync(c => c.ConversationId == conversationId && c.UsuarioID == usuarioId, cancellationToken);
        if (!ownsConversation)
            throw new ResourceNotFoundException("Conversa não encontrada.");

        var safeTake = Math.Clamp(take, 1, 20);
        var messages = await context.ChatMessages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId && m.UsuarioID == usuarioId)
            .OrderByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.MessageId)
            .Take(safeTake)
            .Select(m => new ChatHistoryMessageDto(m.Role, m.Content))
            .ToListAsync(cancellationToken);

        messages.Reverse();
        return messages;
    }

    public async Task<IReadOnlyList<ChatHistoryItemDto>> GetMessagesAsync(
        Guid conversationId, int usuarioId, int take, CancellationToken cancellationToken = default)
    {
        var ownsConversation = await context.ChatConversations.AsNoTracking()
            .AnyAsync(c => c.ConversationId == conversationId && c.UsuarioID == usuarioId, cancellationToken);
        if (!ownsConversation)
            throw new ResourceNotFoundException("Conversa não encontrada.");

        var safeTake = Math.Clamp(take, 1, 100);
        var messages = await context.ChatMessages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId && m.UsuarioID == usuarioId)
            .OrderByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.MessageId)
            .Take(safeTake)
            .Select(m => new ChatHistoryItemDto(m.MessageId, m.Role, m.Content, m.CreatedAt))
            .ToListAsync(cancellationToken);

        messages.Reverse();
        return messages;
    }
}
