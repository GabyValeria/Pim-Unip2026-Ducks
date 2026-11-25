using Ducks.Domain.Entities;
using Ducks.Domain.Exceptions;
using Ducks.Infrastructure.Persistence;
using Ducks.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Ducks.UnitTests;

public sealed class ChatConversationServiceTests
{
    private static DucksDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DucksDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new DucksDbContext(options);
    }

    [Fact]
    public async Task Historico_Deve_Manter_Ordem_E_Isolar_Usuario()
    {
        await using var context = CreateContext();
        var service = new ChatConversationService(context);
        const int ownerId = 42;
        var conversationId = await service.GetOrCreateAsync(ownerId, null);

        await service.AddMessageAsync(conversationId, ownerId, Guid.NewGuid(), "user", "Olá");
        await service.AddMessageAsync(conversationId, ownerId, Guid.NewGuid(), "assistant", "Olá! Como posso ajudar?");

        var history = await service.GetMessagesAsync(conversationId, ownerId, 50);
        Assert.Equal(2, history.Count);
        Assert.Equal("user", history[0].Role);
        Assert.Equal("assistant", history[1].Role);

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => service.GetMessagesAsync(conversationId, ownerId + 1, 50));
    }
}
