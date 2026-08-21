namespace EduPlatform.BuildingBlocks.Application.Abstractions;

/// <summary>
/// The current time, as an injected dependency.
/// </summary>
/// <remarks>
/// Nothing in the domain calls <c>DateTimeOffset.UtcNow</c> directly. Deadlines, test
/// timers and term boundaries all depend on "now", and tests must be able to control it.
/// </remarks>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
