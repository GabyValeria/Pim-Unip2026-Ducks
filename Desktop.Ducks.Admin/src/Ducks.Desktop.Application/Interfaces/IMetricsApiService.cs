using System.Threading.Tasks;
using Ducks.Desktop.Core.Models;

namespace Ducks.Desktop.Application.Interfaces
{
    public interface IMetricsApiService
    {
        Task<DashboardMetricsDto?> GetDashboardMetricsAsync();
    }
}