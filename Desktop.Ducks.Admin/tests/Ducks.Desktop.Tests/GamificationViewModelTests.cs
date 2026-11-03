using System.Collections.Generic;
using System.Threading.Tasks;
using Ducks.Desktop.Application.Interfaces;
using Ducks.Desktop.Application.ViewModels;
using Ducks.Desktop.Core.Models;
using Moq;
using Xunit;

namespace Ducks.Desktop.Tests.ViewModels
{
    public class GamificationViewModelTests
    {
        [Fact]
        public async Task CarregarDadosCommand_PopulatesBadgesAndChallenges()
        {
            // Arrange
            var apiServiceMock = new Mock<IGamificationApiService>();
            apiServiceMock.Setup(s => s.GetBadgesAsync())
                .ReturnsAsync(new List<BadgeDto> { new BadgeDto { Nome = "Explorador", RecompensaXP = 100 } });
            apiServiceMock.Setup(s => s.GetChallengesAsync())
                .ReturnsAsync(new List<ChallengeDto> { new ChallengeDto { Titulo = "Desafio Semanal", Ativo = true } });

            var viewModel = new GamificationManagementViewModel(apiServiceMock.Object);

            // Act
            await viewModel.CarregarDadosCommand.ExecuteAsync(null);

            // Assert
            Assert.Single(viewModel.Badges);
            Assert.Single(viewModel.Desafios);
            Assert.Equal("Explorador", viewModel.Badges[0].Nome);
            Assert.Equal("Desafio Semanal", viewModel.Desafios[0].Titulo);
            Assert.False(viewModel.IsLoading);
        }

        [Fact]
        public async Task AjustarXpCommand_WithValidData_CallsApiAndUpdatesStatus()
        {
            // Arrange
            var apiServiceMock = new Mock<IGamificationApiService>();
            apiServiceMock.Setup(s => s.AdjustUserXpAsync(It.IsAny<UserXpAdjustmentDto>()))
                .ReturnsAsync(true);

            var viewModel = new GamificationManagementViewModel(apiServiceMock.Object)
            {
                TargetUsuarioId = 5,
                QuantidadeXpAjuste = 250,
                MotivoAjuste = "Bônus por mérito"
            };

            // Act
            await viewModel.AjustarXpCommand.ExecuteAsync(null);

            // Assert
            Assert.Contains("sucesso", viewModel.MensagemStatus);
            apiServiceMock.Verify(s => s.AdjustUserXpAsync(It.Is<UserXpAdjustmentDto>(dto => dto.UsuarioID == 5 && dto.QuantidadeXP == 250)), Times.Once);
        }
    }
}