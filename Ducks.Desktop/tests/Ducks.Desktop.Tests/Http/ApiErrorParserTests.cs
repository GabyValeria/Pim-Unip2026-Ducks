using System.Net;
using Ducks.Desktop.Application.Http;

namespace Ducks.Desktop.Tests
{
    public class ApiErrorParserTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ExtractMessage_WithEmptyBody_ReturnsNull(string? body)
        {
            Assert.Null(ApiErrorParser.ExtractMessage(body));
        }

        [Fact]
        public void ExtractMessage_WithPlainText_ReturnsText()
        {
            Assert.Equal("Usuário não encontrado.", ApiErrorParser.ExtractMessage("Usuário não encontrado."));
        }

        [Fact]
        public void ExtractMessage_WithJsonString_ReturnsInnerString()
        {
            Assert.Equal("Erro X", ApiErrorParser.ExtractMessage("\"Erro X\""));
        }

        [Fact]
        public void ExtractMessage_WithMessageProperty_ReturnsMessage()
        {
            Assert.Equal("Falhou", ApiErrorParser.ExtractMessage("{\"message\":\"Falhou\"}"));
        }

        [Fact]
        public void ExtractMessage_WithProblemDetails_PrefersDetailOverTitle()
        {
            var body = "{\"title\":\"Bad Request\",\"detail\":\"Detalhe do problema\"}";
            Assert.Equal("Detalhe do problema", ApiErrorParser.ExtractMessage(body));
        }

        [Fact]
        public void ExtractMessage_WithValidationErrors_JoinsAllMessages()
        {
            var body = "{\"errors\":{\"Nome\":[\"Obrigatório.\",\"Muito curto.\"],\"Email\":[\"Inválido.\"]}}";
            var message = ApiErrorParser.ExtractMessage(body);

            Assert.NotNull(message);
            Assert.Contains("Obrigatório.", message);
            Assert.Contains("Muito curto.", message);
            Assert.Contains("Inválido.", message);
        }

        [Fact]
        public void ExtractMessage_WithHtmlPage_ReturnsNull()
        {
            Assert.Null(ApiErrorParser.ExtractMessage("<html><body>502 Bad Gateway</body></html>"));
        }

        [Fact]
        public void ExtractMessage_WithVeryLongText_IsTruncated()
        {
            var message = ApiErrorParser.ExtractMessage(new string('a', 500));

            Assert.NotNull(message);
            Assert.Equal(401, message!.Length);
            Assert.EndsWith("…", message);
        }

        [Fact]
        public void ExtractMessage_WithJsonWithoutKnownKeys_ReturnsNull()
        {
            Assert.Null(ApiErrorParser.ExtractMessage("{\"foo\":1}"));
        }

        [Theory]
        [InlineData(HttpStatusCode.BadRequest, ApiErrorKind.Validation)]
        [InlineData(HttpStatusCode.UnprocessableEntity, ApiErrorKind.Validation)]
        [InlineData(HttpStatusCode.Conflict, ApiErrorKind.Validation)]
        [InlineData(HttpStatusCode.Unauthorized, ApiErrorKind.Unauthorized)]
        [InlineData(HttpStatusCode.Forbidden, ApiErrorKind.Forbidden)]
        [InlineData(HttpStatusCode.NotFound, ApiErrorKind.NotFound)]
        [InlineData(HttpStatusCode.TooManyRequests, ApiErrorKind.Server)]
        [InlineData(HttpStatusCode.ServiceUnavailable, ApiErrorKind.Server)]
        [InlineData(HttpStatusCode.InternalServerError, ApiErrorKind.Server)]
        [InlineData((HttpStatusCode)418, ApiErrorKind.Unknown)]
        public void FromResponse_MapsStatusCodeToKind(HttpStatusCode status, ApiErrorKind expected)
        {
            var exception = ApiErrorParser.FromResponse(status, null);

            Assert.Equal(expected, exception.Kind);
            Assert.Equal(status, exception.StatusCode);
            Assert.False(string.IsNullOrWhiteSpace(exception.Message));
        }

        [Fact]
        public void FromResponse_BadRequestWithBody_UsesApiMessage()
        {
            var exception = ApiErrorParser.FromResponse(HttpStatusCode.BadRequest, "FimEm deve ser posterior a InicioEm.");

            Assert.Equal("FimEm deve ser posterior a InicioEm.", exception.Message);
        }

        [Fact]
        public void FromResponse_Unauthorized_IgnoresBodyAndUsesFriendlyMessage()
        {
            var exception = ApiErrorParser.FromResponse(HttpStatusCode.Unauthorized, "{\"message\":\"jwt expired\"}");

            Assert.Equal("Sua sessão expirou. Faça login novamente.", exception.Message);
        }

        [Fact]
        public void FromResponse_ServerErrorWithoutBody_UsesDefaultMessage()
        {
            var exception = ApiErrorParser.FromResponse(HttpStatusCode.InternalServerError, null);

            Assert.Contains("servidor", exception.Message);
        }

        [Fact]
        public void FromResponse_ServerError_AppendsTraceIdWhenPresent()
        {
            const string body = "{\"title\":\"Erro interno\",\"status\":500,\"detail\":\"Ocorreu um erro inesperado.\",\"traceId\":\"abc-123\"}";

            var exception = ApiErrorParser.FromResponse(HttpStatusCode.InternalServerError, body);

            Assert.Contains("Ocorreu um erro inesperado.", exception.Message);
            Assert.Contains("(ocorrência: abc-123)", exception.Message);
        }

        [Fact]
        public void FromResponse_ClientError_DoesNotAppendTraceId()
        {
            const string body = "{\"message\":\"Dados inválidos.\",\"traceId\":\"abc-123\"}";

            var exception = ApiErrorParser.FromResponse(HttpStatusCode.BadRequest, body);

            Assert.DoesNotContain("ocorrência", exception.Message);
        }

        [Fact]
        public void ExtractTraceId_WithoutField_ReturnsNull()
        {
            Assert.Null(ApiErrorParser.ExtractTraceId("{\"message\":\"x\"}"));
            Assert.Null(ApiErrorParser.ExtractTraceId("texto"));
            Assert.Null(ApiErrorParser.ExtractTraceId(null));
        }
    }
}