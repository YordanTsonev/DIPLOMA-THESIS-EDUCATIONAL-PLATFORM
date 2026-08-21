namespace EduPlatform.BuildingBlocks.Application.Messaging;

public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken);
}

public interface ICommandHandler<in TCommand> : ICommandHandler<TCommand, Unit>
    where TCommand : ICommand;

public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken);
}

/// <summary>
/// Base class for handlers of commands that return nothing, so implementers
/// override a plain <c>Task</c> method instead of returning <see cref="Unit"/> by hand.
/// </summary>
public abstract class CommandHandler<TCommand> : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    public async Task<Unit> HandleAsync(TCommand command, CancellationToken cancellationToken)
    {
        await HandleCoreAsync(command, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }

    protected abstract Task HandleCoreAsync(TCommand command, CancellationToken cancellationToken);
}
