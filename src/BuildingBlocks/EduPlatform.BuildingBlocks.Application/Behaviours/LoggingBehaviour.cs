using System.Diagnostics;
using EduPlatform.BuildingBlocks.Application.Messaging;
using Microsoft.Extensions.Logging;

namespace EduPlatform.BuildingBlocks.Application.Behaviours;

/// <summary>
/// Records the name and duration of every dispatched message. Together with the
/// correlation id added by the API middleware this gives one traceable line per use case.
/// </summary>
internal sealed partial class LoggingBehaviour<TMessage, TResult>(
    ILogger<LoggingBehaviour<TMessage, TResult>> logger)
    : IPipelineBehaviour<TMessage, TResult>
{
    public async Task<TResult> HandleAsync(
        TMessage message,
        Func<Task<TResult>> continuation,
        CancellationToken cancellationToken)
    {
        var messageName = typeof(TMessage).Name;
        var timestamp = Stopwatch.GetTimestamp();

        try
        {
            var result = await continuation().ConfigureAwait(false);
            var elapsedMs = Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds;
            Handled(logger, messageName, elapsedMs);
            return result;
        }
        catch (Exception exception)
        {
            var elapsedMs = Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds;
            Failed(logger, messageName, elapsedMs, exception);
            throw;
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Handled {MessageName} in {ElapsedMs:0.##} ms")]
    private static partial void Handled(ILogger logger, string messageName, double elapsedMs);

    [LoggerMessage(Level = LogLevel.Error, Message = "{MessageName} failed after {ElapsedMs:0.##} ms")]
    private static partial void Failed(ILogger logger, string messageName, double elapsedMs, Exception exception);
}
