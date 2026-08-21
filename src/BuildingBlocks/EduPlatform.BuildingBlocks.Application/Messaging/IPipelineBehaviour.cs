namespace EduPlatform.BuildingBlocks.Application.Messaging;

/// <summary>
/// Cross-cutting step wrapped around every handler — validation, logging, transactions.
/// Behaviours run in registration order; call <paramref name="continuation"/> to continue the chain.
/// </summary>
public interface IPipelineBehaviour<in TMessage, TResult>
{
    Task<TResult> HandleAsync(
        TMessage message,
        Func<Task<TResult>> continuation,
        CancellationToken cancellationToken);
}
