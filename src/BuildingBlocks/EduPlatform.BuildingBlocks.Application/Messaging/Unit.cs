namespace EduPlatform.BuildingBlocks.Application.Messaging;

/// <summary>
/// Stands in for "no result", so commands that return nothing and commands that
/// return a value can share one handler interface and one dispatch path.
/// </summary>
public readonly record struct Unit
{
    public static readonly Unit Value;
}
