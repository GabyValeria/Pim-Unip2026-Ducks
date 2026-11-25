using System.Security.Claims;
using Ducks.Application.DTOs;
using Ducks.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Ducks.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/chat")]
public sealed class ChatController(
    IDuckBotService duckBotService,
    IChatConversationService conversationService) : ControllerBase
{
    [HttpPost("messages")]
    [EnableRateLimiting("ChatPerUser")]
    public async Task<IActionResult> SendMessage(
        [FromBody] ChatMessageRequestDto dto,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var userMessage = dto.Message.Trim();
        var conversationId = await conversationService.GetOrCreateAsync(userId, dto.ConversationId, cancellationToken);
        await conversationService.AddMessageAsync(
            conversationId, userId, Guid.NewGuid(), "user", userMessage, cancellationToken);

        // Recupera somente mensagens desta conversa e deste usuário. Nunca aceita histórico enviado pelo cliente.
        var history = await conversationService.GetRecentMessagesAsync(conversationId, userId, 12, cancellationToken);
        var request = new DuckBotRequest(
            conversationId,
            userMessage,
            dto.ModuleId,
            dto.LessonId,
            userId,
            history);

        var response = await duckBotService.SendMessageAsync(request, cancellationToken);
        await conversationService.AddMessageAsync(
            conversationId, userId, response.MessageId, "assistant", response.Answer, cancellationToken);

        return Ok(response);
    }

    [HttpGet("conversations/{conversationId:guid}/messages")]
    public async Task<IActionResult> GetMessages(
        Guid conversationId,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var messages = await conversationService.GetMessagesAsync(
            conversationId, userId, Math.Clamp(take, 1, 100), cancellationToken);
        return Ok(messages);
    }

    private bool TryGetUserId(out int userId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}
