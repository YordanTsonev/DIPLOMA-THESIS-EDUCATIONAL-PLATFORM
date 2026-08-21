using EduPlatform.BuildingBlocks.Domain;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace EduPlatform.Api.Middleware;

/// <summary>
/// Turns every unhandled exception into an RFC 9457 problem document.
/// </summary>
/// <remarks>
/// Only the exception types the client can act on are translated to a 4xx status. Anything
/// else becomes a 500 with a generic message — internal details go to the log, never to the
/// browser, so a stack trace cannot leak schema or file-system information to a student.
/// </remarks>
internal sealed partial class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problemDetails = exception switch
        {
            ValidationException validation => CreateValidationProblem(validation),
            DomainException domain => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "The request conflicts with the current state.",
                Detail = domain.Message,
            },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                Detail = "The error has been logged. Quote the trace id when reporting it.",
            },
        };

        if (problemDetails.Status == StatusCodes.Status500InternalServerError)
        {
            Unhandled(logger, httpContext.TraceIdentifier, exception);
        }
        else
        {
            Handled(logger, httpContext.TraceIdentifier, exception.Message);
        }

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails,
        }).ConfigureAwait(false);
    }

    private static ValidationProblemDetails CreateValidationProblem(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(failure => failure.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).ToArray(),
                StringComparer.Ordinal);

        return new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred.",
        };
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception on request {TraceId}")]
    private static partial void Unhandled(ILogger logger, string traceId, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Request {TraceId} rejected: {Reason}")]
    private static partial void Handled(ILogger logger, string traceId, string reason);
}
