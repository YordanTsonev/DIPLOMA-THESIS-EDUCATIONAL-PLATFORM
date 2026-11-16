using EduPlatform.BuildingBlocks.Application.Messaging;
using EduPlatform.Modules.Assessment.Application.UseCases;

namespace EduPlatform.Api.Hosting;

internal sealed partial class ExpiredAttemptSweeper(
    IServiceScopeFactory scopes,
    ILogger<ExpiredAttemptSweeper> logger) : BackgroundService
{
    private const int BatchSize = 200;

    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(60);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();

                var finalized = await scope.ServiceProvider
                    .GetRequiredService<IDispatcher>()
                    .SendAsync(new FinalizeExpiredAttemptsCommand(BatchSize), stoppingToken)
                    .ConfigureAwait(false);

                if (finalized > 0)
                {
                    Finalized(logger, finalized);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception exception)
            {
                Failed(logger, exception);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Finalized {Count} expired test attempts")]
    private static partial void Finalized(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Error, Message = "Finalizing expired test attempts failed")]
    private static partial void Failed(ILogger logger, Exception exception);
}
