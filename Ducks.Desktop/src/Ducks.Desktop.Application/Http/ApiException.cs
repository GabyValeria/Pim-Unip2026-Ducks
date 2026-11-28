using System;
using System.Net;

namespace Ducks.Desktop.Application.Http
{
    public enum ApiErrorKind
    {
        Network,
        Timeout,
        Unauthorized,
        Forbidden,
        NotFound,
        Validation,
        Server,
        Unknown
    }

    /// <summary>Erro de API já traduzido para uma mensagem amigável em português (Message).</summary>
    public class ApiException : Exception
    {
        public ApiErrorKind Kind { get; }
        public HttpStatusCode? StatusCode { get; }

        public ApiException(ApiErrorKind kind, string message, HttpStatusCode? statusCode = null, Exception? inner = null)
            : base(message, inner)
        {
            Kind = kind;
            StatusCode = statusCode;
        }
    }
}
