using System.Threading;
using System.Threading.Tasks;

namespace Ducks.Desktop.Application.Http
{
    /// <summary>
    /// Cliente HTTP único do Desktop: injeta o JWT, renova o token uma vez em caso de 401
    /// e converte qualquer falha em <see cref="ApiException"/> com mensagem amigável.
    /// </summary>
    public interface IApiClient
    {
        Task<T?> GetAsync<T>(string url, CancellationToken cancellationToken = default);
        Task<T?> PostAsync<T>(string url, object? body, CancellationToken cancellationToken = default);
        Task PostAsync(string url, object? body, CancellationToken cancellationToken = default);
        Task<T?> PutAsync<T>(string url, object? body, CancellationToken cancellationToken = default);
        Task PutAsync(string url, object? body, CancellationToken cancellationToken = default);
        Task DeleteAsync(string url, CancellationToken cancellationToken = default);
    }
}
