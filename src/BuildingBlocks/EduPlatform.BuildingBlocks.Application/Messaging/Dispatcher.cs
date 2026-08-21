using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace EduPlatform.BuildingBlocks.Application.Messaging;

/// <summary>
/// Default <see cref="IDispatcher"/>. Resolves the one handler registered for a message
/// and wraps it in the registered pipeline behaviours.
/// </summary>
/// <remarks>
/// The message type is only known at runtime, so dispatch goes through a small generic
/// wrapper whose closed type is built once per message type and then cached. The
/// reflection cost is paid on the first message of each type, never afterwards.
/// </remarks>
internal sealed class Dispatcher(IServiceProvider services) : IDispatcher
{
    private static readonly ConcurrentDictionary<Type, object> CommandWrappers = new();
    private static readonly ConcurrentDictionary<Type, object> QueryWrappers = new();

    public Task<TResult> SendAsync<TResult>(
        ICommand<TResult> command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var wrapper = (CommandWrapper<TResult>)CommandWrappers.GetOrAdd(
            command.GetType(),
            static (commandType, resultType) => Activator.CreateInstance(
                typeof(CommandWrapper<,>).MakeGenericType(commandType, resultType))!,
            typeof(TResult));

        return wrapper.HandleAsync(command, services, cancellationToken);
    }

    public Task SendAsync(ICommand command, CancellationToken cancellationToken = default) =>
        SendAsync<Unit>(command, cancellationToken);

    public Task<TResult> QueryAsync<TResult>(
        IQuery<TResult> query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var wrapper = (QueryWrapper<TResult>)QueryWrappers.GetOrAdd(
            query.GetType(),
            static (queryType, resultType) => Activator.CreateInstance(
                typeof(QueryWrapper<,>).MakeGenericType(queryType, resultType))!,
            typeof(TResult));

        return wrapper.HandleAsync(query, services, cancellationToken);
    }

    /// <summary>
    /// Builds the behaviour chain around a handler call. Behaviours are reversed so that
    /// the first one registered ends up outermost — the order you read in Program.cs is
    /// the order they run in.
    /// </summary>
    private static Func<Task<TResult>> BuildPipeline<TMessage, TResult>(
        TMessage message,
        Func<Task<TResult>> handlerCall,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        var behaviours = services.GetServices<IPipelineBehaviour<TMessage, TResult>>().ToArray();
        var next = handlerCall;

        for (var i = behaviours.Length - 1; i >= 0; i--)
        {
            var behaviour = behaviours[i];
            var continuation = next;
            next = () => behaviour.HandleAsync(message, continuation, cancellationToken);
        }

        return next;
    }

    private abstract class CommandWrapper<TResult>
    {
        public abstract Task<TResult> HandleAsync(
            object command,
            IServiceProvider services,
            CancellationToken cancellationToken);
    }

    private sealed class CommandWrapper<TCommand, TResult> : CommandWrapper<TResult>
        where TCommand : ICommand<TResult>
    {
        public override Task<TResult> HandleAsync(
            object command,
            IServiceProvider services,
            CancellationToken cancellationToken)
        {
            var typed = (TCommand)command;
            var handler = services.GetRequiredService<ICommandHandler<TCommand, TResult>>();

            return BuildPipeline<TCommand, TResult>(
                typed,
                () => handler.HandleAsync(typed, cancellationToken),
                services,
                cancellationToken)();
        }
    }

    private abstract class QueryWrapper<TResult>
    {
        public abstract Task<TResult> HandleAsync(
            object query,
            IServiceProvider services,
            CancellationToken cancellationToken);
    }

    private sealed class QueryWrapper<TQuery, TResult> : QueryWrapper<TResult>
        where TQuery : IQuery<TResult>
    {
        public override Task<TResult> HandleAsync(
            object query,
            IServiceProvider services,
            CancellationToken cancellationToken)
        {
            var typed = (TQuery)query;
            var handler = services.GetRequiredService<IQueryHandler<TQuery, TResult>>();

            return BuildPipeline<TQuery, TResult>(
                typed,
                () => handler.HandleAsync(typed, cancellationToken),
                services,
                cancellationToken)();
        }
    }
}
