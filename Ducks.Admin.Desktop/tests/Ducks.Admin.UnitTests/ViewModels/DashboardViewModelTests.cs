using Ducks.Admin.Desktop.Services;
using Ducks.Admin.Desktop.ViewModels;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace Ducks.Admin.UnitTests.ViewModels
{
    public class DashboardViewModelTests
    {
        [Fact]
        public async Task LoadMetricsCommand_ShouldPopulateDashboardMetrics()
        {
            // Arrange
            var mockSignalR = new Mock<ISignalRService>();
            var mockApi = new Mock<IApiClient>();
            var vm = new DashboardViewModel(mockSignalR.Object, mockApi.Object);

            // Act
            await vm.LoadMetricsCommand.ExecuteAsync(null);

            // Assert
            Assert.Equal(1250, vm.Metrics.TotalUsuariosAtivos);
            Assert.Equal(24, vm.Metrics.TotalCursosPublicados);
            Assert.Equal(3, vm.Metrics.ItensPendentesModeracao);
        }
    }
}