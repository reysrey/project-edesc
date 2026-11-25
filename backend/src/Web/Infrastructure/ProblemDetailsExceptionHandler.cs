using System.Text.Json;
using KaizenBoard.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KaizenBoard.Web.Infrastructure;

/// <summary>
/// Converts well-known application exceptions into RFC 9110-compliant <see cref="ProblemDetails"/> responses,
/// mapping <see cref="ValidationException"/> в†’ 400, <see cref="NotFoundException"/> в†’ 404,
/// <see cref="UnauthorizedAccessException"/> в†’ 401, and <see cref="ForbiddenAccessException"/> в†’ 403.
/// Unrecognised exceptions are not handled and fall through to the default middleware.
/// </summary>
public class ProblemDetailsExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, problemDetails) = exception switch
        {
            ValidationException ve => (StatusCodes.Status400BadRequest, (ProblemDetails)new ValidationProblemDetails(ve.Errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1"
            }),
            NotFoundException ne => (StatusCodes.Status404NotFound, new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                Title = "The specified resource was not found.",
                Detail = ne.Message
            }),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.2"
            }),
            ForbiddenAccessException => (StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Forbidden",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.4"
            }),
            BadHttpRequestException badRequest => (badRequest.StatusCode, new ProblemDetails
            {
                Status = badRequest.StatusCode,
                Title = "Invalid request",
                Detail = badRequest.StatusCode == StatusCodes.Status413PayloadTooLarge
                    ? "Размер запроса превышает допустимый предел."
                    : "Не удалось прочитать тело запроса. Проверьте JSON и значения полей."
            }),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict",
                Detail = "Запись уже изменена. Откройте её заново и повторите действие."
            }),
            InvalidOperationException => (StatusCodes.Status409Conflict, new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "InvalidOperation",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.10",
                Detail = exception.Message
            }),
            DbUpdateException => (StatusCodes.Status409Conflict, new ProblemDetails 
            { Status = 409, 
                Title = "Conflict", 
                Detail = "Запись с такими данными уже существует." 
            }),
            JsonException => (StatusCodes.Status400BadRequest, new ProblemDetails 
            { Status = 400, 
                Title = "Bad Request", 
                Detail = "Некорректный формат JSON" 
            }),
            ArgumentException => (StatusCodes.Status400BadRequest, new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Argument",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                Detail = exception.Message
            }),
            _ => (-1, null)

        };

        if (problemDetails is null) return false;

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
