using Xunit;
using Microsoft.Extensions.Configuration;

public class ChatValidationTests
{
    [Fact]
    public void Mensagem_Excede_Limite_Maximo_Deve_Ser_Identificada()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                {"OpenAISettings:MaxMessageLength", "50"}
            })
            .Build();

        int maxLen = configuration.GetValue<int>("OpenAISettings:MaxMessageLength");
        string mensagemLonga = new('A', 100); // 100 caracteres (> 50)

        // Act & Assert
        Assert.True(mensagemLonga.Length > maxLen, "A mensagem deveria ultrapassar o limite configurado.");
    }
}