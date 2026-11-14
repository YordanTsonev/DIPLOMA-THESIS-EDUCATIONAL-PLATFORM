using EduPlatform.BuildingBlocks.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace EduPlatform.Modules.Homework.Application.Abstractions;

internal static partial class OrphanFiles
{
    public static async Task DiscardAsync(
        IFileStorage storage,
        ILogger logger,
        string? storageKey,
        CancellationToken cancellationToken)
    {
        if (storageKey is null)
        {
            return;
        }

        try
        {
            await storage.DeleteAsync(StorageBuckets.Submissions, storageKey, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            Failed(logger, storageKey, exception);
        }
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "Could not remove the stored file {StorageKey}; it is now orphaned.")]
    private static partial void Failed(ILogger logger, string storageKey, Exception exception);
}
