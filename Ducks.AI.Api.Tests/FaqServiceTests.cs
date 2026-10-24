using Xunit;
using Ducks.AI.Api.Services;

public class FaqServiceTests
{
    private readonly FaqService _faqService = new();

    [Theory]
    [InlineData("Como funciona o XP?")]
    [InlineData("tem ranking na plataforma?")]
    [InlineData("esqueci a senha")]
    [InlineData("O que é o Ducks?")]
    public void FaqService_Deve_Retornar_Resposta_Para_Perguntas_Conhecidas(string pergunta)
    {
        // Act
        var resposta = _faqService.BuscarRespostaPronta(pergunta);

        // Assert
        Assert.NotNull(resposta);
        Assert.NotEmpty(resposta);
    }

    [Fact]
    public void FaqService_Deve_Retornar_Null_Para_Perguntas_Desconhecidas()
    {
        // Arrange
        string perguntaPedagogica = "Explique o conceito de Injeção de Dependência em C#.";

        // Act
        var resposta = _faqService.BuscarRespostaPronta(perguntaPedagogica);

        // Assert
        Assert.Null(resposta); // Deve retornar null para que o fluxo siga para a OpenAI
    }
}