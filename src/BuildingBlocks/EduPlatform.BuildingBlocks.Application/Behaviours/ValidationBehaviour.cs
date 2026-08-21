using EduPlatform.BuildingBlocks.Application.Messaging;
using FluentValidation;

namespace EduPlatform.BuildingBlocks.Application.Behaviours;

/// <summary>
/// Runs every registered validator for a message before its handler is reached,
/// so handlers never have to re-check their own input.
/// </summary>
internal sealed class ValidationBehaviour<TMessage, TResult>(
    IEnumerable<IValidator<TMessage>> validators)
    : IPipelineBehaviour<TMessage, TResult>
{
    public async Task<TResult> HandleAsync(
        TMessage message,
        Func<Task<TResult>> continuation,
        CancellationToken cancellationToken)
    {
        var applicable = validators as IValidator<TMessage>[] ?? [.. validators];
        if (applicable.Length == 0)
        {
            return await continuation().ConfigureAwait(false);
        }

        var context = new ValidationContext<TMessage>(message);

        var results = await Task.WhenAll(
            applicable.Select(validator => validator.ValidateAsync(context, cancellationToken)))
            .ConfigureAwait(false);

        var failures = results.SelectMany(result => result.Errors).Where(failure => failure is not null).ToArray();
        if (failures.Length > 0)
        {
            // Translated into an RFC 9457 problem document by the API's exception handler.
            throw new ValidationException(failures);
        }

        return await continuation().ConfigureAwait(false);
    }
}
