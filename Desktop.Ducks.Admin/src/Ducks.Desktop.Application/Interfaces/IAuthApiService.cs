using System.Threading.Tasks;
using Ducks.Desktop.Core.Models;

namespace Ducks.Desktop.Application.Interfaces
{
    public interface IAuthApiService
    {
        Task<AuthResponseDto?> LoginAsync(string email, string senha);
        string? CurrentToken { get; set; }
    }
}