using System.Net;
using System.Text.Json;

namespace Ducks.Desktop.Application.Http
{
    /// <summary>Traduz respostas de erro da API (string, ProblemDetails, { message }) em ApiException.</summary>
    public static class ApiErrorParser
    {
        public static ApiException FromResponse(HttpStatusCode status, string? body)
        {
            var apiMessage = ExtractMessage(body);
            var code = (int)status;
            var traceId = code >= 500 ? ExtractTraceId(body) : null;
            if (traceId != null)
                apiMessage = (apiMessage ?? "O servidor encontrou um erro ao processar a solicitação.") + $" (ocorrência: {traceId})";

            return code switch
            {
                400 or 422 => new ApiException(ApiErrorKind.Validation,
                    apiMessage ?? "Os dados informados são inválidos. Revise o formulário e tente novamente.", status),
                401 => new ApiException(ApiErrorKind.Unauthorized,
                    "Sua sessão expirou. Faça login novamente.", status),
                403 => new ApiException(ApiErrorKind.Forbidden,
                    "Você não tem permissão para realizar esta ação.", status),
                404 => new ApiException(ApiErrorKind.NotFound,
                    apiMessage ?? "O registro solicitado não foi encontrado.", status),
                409 => new ApiException(ApiErrorKind.Validation,
                    apiMessage ?? "Esta operação conflita com o estado atual do registro.", status),
                429 => new ApiException(ApiErrorKind.Server,
                    "Muitas requisições em pouco tempo. Aguarde alguns instantes e tente novamente.", status),
                503 => new ApiException(ApiErrorKind.Server,
                    apiMessage ?? "O serviço está temporariamente indisponível.", status),
                >= 500 => new ApiException(ApiErrorKind.Server,
                    apiMessage ?? "O servidor encontrou um erro ao processar a solicitação. Tente novamente em instantes.", status),
                _ => new ApiException(ApiErrorKind.Unknown,
                    apiMessage ?? $"A API retornou um erro inesperado (HTTP {code}).", status)
            };
        }

        /// <summary>Lê o identificador da ocorrência (traceId/correlationId) do corpo de erros 5xx, se houver.</summary>
        public static string? ExtractTraceId(string? body)
        {
            if (string.IsNullOrWhiteSpace(body) || !body.TrimStart().StartsWith('{')) return null;
            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.ValueKind != JsonValueKind.Object) return null;
                foreach (var key in new[] { "traceId", "correlationId", "identificador", "requestId" })
                {
                    if (TryGet(doc.RootElement, key, out var value) && value.ValueKind == JsonValueKind.String
                        && !string.IsNullOrWhiteSpace(value.GetString()))
                        return Limit(value.GetString());
                }
            }
            catch (JsonException) { }
            return null;
        }

        public static string? ExtractMessage(string? body)
        {
            if (string.IsNullOrWhiteSpace(body)) return null;
            var text = body.Trim();

            if (text.StartsWith('{') || text.StartsWith('"'))
            {
                try
                {
                    using var doc = JsonDocument.Parse(text);
                    var root = doc.RootElement;

                    if (root.ValueKind == JsonValueKind.String)
                        return Limit(root.GetString());

                    if (root.ValueKind == JsonValueKind.Object)
                    {
                        // Erros de validação do ASP.NET: { errors: { Campo: ["msg"] } }
                        if (TryGet(root, "errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                        {
                            var parts = new List<string>();
                            foreach (var field in errors.EnumerateObject())
                            {
                                if (field.Value.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (var msg in field.Value.EnumerateArray())
                                        if (msg.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(msg.GetString()))
                                            parts.Add(msg.GetString()!);
                                }
                                else if (field.Value.ValueKind == JsonValueKind.String)
                                {
                                    parts.Add(field.Value.GetString()!);
                                }
                            }
                            if (parts.Count > 0) return Limit(string.Join(" ", parts));
                        }

                        foreach (var key in new[] { "message", "mensagem", "mensagemErro", "detail", "title", "error" })
                        {
                            if (TryGet(root, key, out var value) && value.ValueKind == JsonValueKind.String
                                && !string.IsNullOrWhiteSpace(value.GetString()))
                                return Limit(value.GetString());
                        }
                    }
                    return null;
                }
                catch (JsonException)
                {
                    // não era JSON válido: cai para texto simples
                }
            }

            // Texto simples (ex.: BadRequest("mensagem")). Ignora páginas HTML de erro.
            if (text.StartsWith('<')) return null;
            return Limit(text);
        }

        private static bool TryGet(JsonElement obj, string name, out JsonElement value)
        {
            foreach (var p in obj.EnumerateObject())
            {
                if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = p.Value;
                    return true;
                }
            }
            value = default;
            return false;
        }

        private static string? Limit(string? s) =>
            string.IsNullOrWhiteSpace(s) ? null : (s.Length > 400 ? s[..400] + "…" : s.Trim());
    }
}