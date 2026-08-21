using EduPlatform.BuildingBlocks.Application.Abstractions;

namespace EduPlatform.BuildingBlocks.Infrastructure.Time;

internal sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
