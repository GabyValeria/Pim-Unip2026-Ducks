using Xunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ducks.AI.Api.Services;
using Ducks.AI.Api.Models;
using Moq;

public class SecurityTests
{
    [Fact]
    public void PedagogicalChatService_Deve_Lancar_Excecao_Se_ApiKey_Estiver_Vazia()
    {
        // Arrange
        var options = Options.Create(new OpenAIOptions
        {
            ApiKey = string.Empty,
            Model = "gpt-4o-mini"
        });
        
        var faqServiceMock = new Mock<FaqService>();
        var loggerMock = new Mock<ILogger<PedagogicalChatService>>();

        // Act & Assert (Fornecendo todos os parâmetros exigidos pelo construtor atualizado)
        var exception = Assert.Throws<InvalidOperationException>(() => 
            new PedagogicalChatService(options, faqServiceMock.Object, loggerMock.Object));
        
        Assert.Contains("OpenAI API Key não configurada", exception.Message);
    }
}