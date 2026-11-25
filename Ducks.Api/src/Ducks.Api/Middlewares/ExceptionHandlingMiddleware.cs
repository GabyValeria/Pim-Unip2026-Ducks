using Ducks.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Ducks.Api.Middlewares;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // O cliente encerrou a requisição. Não transforma cancelamento em erro 500.
            throw;
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
                throw;

            var (status, title, detail) = exception switch
            {
                ResourceNotFoundException => (StatusCodes.Status404NotFound, "Recurso não encontrado", exception.Message),
                BusinessRuleException => (StatusCodes.Status400BadRequest, "Regra de negócio não atendida", exception.Message),
                ConflictException => (StatusCodes.Status409Conflict, "Conflito", exception.Message),
                UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Não autenticado", "Credenciais inválidas ou sessão não autorizada."),
                ArgumentException => (StatusCodes.Status400BadRequest, "Requisição inválida", "Verifique os campos e parâmetros informados."),
                DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Conflito de concorrência", "O registro foi alterado por outra operação. Atualize os dados e tente novamente."),
                // Acrescentado: chave única repetida (requisição duplicada) e exclusão de item que ainda tem vínculos.
                DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 } } => (StatusCodes.Status409Conflict, "Conflito", "O registro já existe. A operação pode ter sido enviada duas vezes."),
                DbUpdateException { InnerException: SqlException { Number: 547 } } => (StatusCodes.Status409Conflict, "Conflito", "O registro está vinculado a outros dados e não pode ser alterado ou excluído."),
                TimeoutException => (StatusCodes.Status503ServiceUnavailable, "Serviço temporariamente indisponível", "A operação excedeu o tempo limite."),
                _ => (StatusCodes.Status500InternalServerError, "Erro interno", "Ocorreu um erro inesperado. Informe o identificador da ocorrência ao suporte.")
            };

            if (status >= 500)
                logger.LogError(exception, "Falha não tratada. TraceId: {TraceId}", context.TraceIdentifier);
            else
                logger.LogInformation("Requisição rejeitada com {Status}. TraceId: {TraceId}", status, context.TraceIdentifier);

            context.Response.Clear();
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail,
                Instance = context.Request.Path
            };
            problem.Extensions["traceId"] = context.TraceIdentifier;
            await context.Response.WriteAsJsonAsync(problem, cancellationToken: context.RequestAborted);
        }
    }
}
