namespace EduPlatform.BuildingBlocks.Application.Messaging;

/// <summary>A request that changes state and returns a result.</summary>
public interface ICommand<TResult>;

/// <summary>A request that changes state and returns nothing.</summary>
public interface ICommand : ICommand<Unit>;

/// <summary>A read-only request. Must not change state.</summary>
public interface IQuery<TResult>;
