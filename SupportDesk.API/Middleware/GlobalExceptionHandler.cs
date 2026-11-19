using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Exceptions;
using SupportDesk.Domain.Exceptions;

namespace SupportDesk.API.Middleware;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken ct)
    {
        var (status, title, detail) = exception switch
        {
            DomainException e => (StatusCodes.Status422UnprocessableEntity, "Business rule violated", e.Message),
            ConflictException e => (StatusCodes.Status409Conflict, "Conflict", e.Message),
            NotFoundException e => (StatusCodes.Status404NotFound, "Not found", e.Message),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Concurrency conflict",
        "The resource was modified by another request. Reload it and try again."),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected error",
                                    "An unexpected error occurred.") 
        };

        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception");

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        }, ct);

        return true;
    }
}