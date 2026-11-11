using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Transfers.Application.Common;
using Transfers.Domain.Common;

namespace Transfers.Api.Errors;

public sealed class ApiExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception exception, CancellationToken ct)
    {
        var (status, code, title) = exception switch
        {
            NotFoundException e => (StatusCodes.Status404NotFound, e.Code, "Recurso não encontrado"),
            ConflictException e => (StatusCodes.Status409Conflict, e.Code, "Conflito"),
            ForbiddenException e => (StatusCodes.Status403Forbidden, e.Code, "Acesso negado"),
            DomainException { Code: "transfer.not_cancellable" } e =>
                (StatusCodes.Status409Conflict, e.Code, "Estado não permite a operação"),
            DomainException { Code: "account.inactive" } e =>
                (StatusCodes.Status422UnprocessableEntity, e.Code, "Conta bloqueada ou inativa"),
            DomainException e => (StatusCodes.Status400BadRequest, e.Code, "Requisição inválida"),
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } } =>
                (StatusCodes.Status409Conflict, "idempotency.conflict", "Idempotency-Key já utilizada"),
            PostgresException { SqlState: PostgresErrorCodes.LockNotAvailable } =>
                (StatusCodes.Status503ServiceUnavailable, "account.busy", "Conta ocupada, tente novamente"),
            _ => (0, "", "")
        };

        if (status == 0)
            return false;

        http.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = exception.Message,
                Extensions = { ["code"] = code }
            }
        });
    }
}
