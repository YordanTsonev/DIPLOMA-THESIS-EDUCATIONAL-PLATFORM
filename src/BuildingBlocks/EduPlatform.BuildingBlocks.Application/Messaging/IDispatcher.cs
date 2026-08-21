namespace EduPlatform.BuildingBlocks.Application.Messaging;

/// <summary>
/// Routes a command or query to its single handler, through the behaviour pipeline.
/// </summary>
/// <remarks>
/// Hand-written rather than taken from a mediator library. The whole contract is three
/// methods, and it keeps the project free of the reciprocal-licence obligations that
/// current MediatR releases carry.
/// </remarks>
public interface IDispatcher
{
    Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default);

    Task SendAsync(ICommand command, CancellationToken cancellationToken = default);

    Task<TResult> QueryAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default);
}
