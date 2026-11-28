using System.Net;
using System.Text;

namespace Ducks.Desktop.Tests
{
    /// <summary>Requisição registrada pelo stub (capturada no momento do envio, antes de o ApiClient descartá-la).</summary>
    public sealed record RecordedCall(HttpMethod Method, string Uri, string? Authorization, string? Body);

    /// <summary>
    /// HttpMessageHandler de teste: devolve respostas enfileiradas em ordem e registra cada chamada.
    /// Evita rede real e permite simular 401, 5xx, timeout e queda de conexão.
    /// </summary>
    public sealed class StubHttpHandler : HttpMessageHandler
    {
        private readonly Queue<Func<HttpResponseMessage>> _responses = new();

        public List<RecordedCall> Calls { get; } = new();

        public StubHttpHandler Enqueue(HttpStatusCode status, string? json = null)
        {
            _responses.Enqueue(() => new HttpResponseMessage(status)
            {
                Content = json == null ? null : new StringContent(json, Encoding.UTF8, "application/json")
            });
            return this;
        }

        public StubHttpHandler EnqueueException(Exception exception)
        {
            _responses.Enqueue(() => throw exception);
            return this;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content?.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();
            Calls.Add(new RecordedCall(request.Method, request.RequestUri!.ToString(), request.Headers.Authorization?.ToString(), body));

            if (_responses.Count == 0)
                throw new InvalidOperationException("Nenhuma resposta configurada no StubHttpHandler para esta chamada.");

            return Task.FromResult(_responses.Dequeue()());
        }

        public HttpClient CreateClient() => new(this) { BaseAddress = new Uri("http://localhost/") };
    }
}
