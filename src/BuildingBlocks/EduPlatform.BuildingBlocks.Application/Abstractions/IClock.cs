namespace EduPlatform.BuildingBlocks.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
