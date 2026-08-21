namespace EduPlatform.Api.Middleware;

/// <summary>
/// Gives every request a correlation id, echoes it back in the response and puts it in the
/// log scope, so one identifier ties together the browser call, the API log lines and the
/// database work it caused.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context, ILogger<CorrelationIdMiddleware> logger)
    {
        ArgumentNullException.ThrowIfNull(context);

        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var provided)
            && !string.IsNullOrWhiteSpace(provided)
                ? provided.ToString()
                : Guid.CreateVersion7().ToString();

        context.TraceIdentifier = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await next(context).ConfigureAwait(false);
        }
    }
}
