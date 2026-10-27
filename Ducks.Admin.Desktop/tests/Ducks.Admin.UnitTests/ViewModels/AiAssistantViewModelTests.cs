using Ducks.Admin.Desktop.Services;
using Ducks.Admin.Desktop.ViewModels;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace Ducks.Admin.UnitTests.ViewModels
{
    public class AiAssistantViewModelTests
    {
        [Fact]
        public async Task SummarizeModerationCommand_ShouldUpdateAiResponse()
        {
            // Arrange
            var mockAiService = new Mock<IAiService>();
            mockAiService.Setup(s => s.SummarizeModerationQueueAsync(It.IsAny<string>()))
                         .ReturnsAsync("Resumo gerado por IA com sucesso.");

            var vm = new AiAssistantViewModel(mockAiService.Object);

            // Act
            await vm.SummarizeModerationCommand.ExecuteAsync(null);

            // Assert
            Assert.Equal("Resumo gerado por IA com sucesso.", vm.AiResponse);
            Assert.Equal("Sumarização concluída com sucesso.", vm.StatusMessage);
        }

        [Fact]
        public async Task GenerateSuggestionsCommand_WithEmptyInput_ShouldPromptWarning()
        {
            // Arrange
            var mockAiService = new Mock<IAiService>();
            var vm = new AiAssistantViewModel(mockAiService.Object)
            {
                InputPrompt = string.Empty
            };

            // Act
            await vm.GenerateSuggestionsCommand.ExecuteAsync(null);

            // Assert
            Assert.Equal("Digite um tema para gerar sugestões.", vm.StatusMessage);
            mockAiService.Verify(s => s.GenerateActivitySuggestionsAsync(It.IsAny<string>()), Times.Never);
        }
    }
}