using System.Globalization;
using EduPlatform.BuildingBlocks.Domain;

namespace EduPlatform.Modules.Identity.Domain;

/// <summary>
/// A validated e-mail address. It doubles as the login identifier, so it is normalised on
/// creation: two users cannot register as <c>Ivan@example.com</c> and <c>ivan@example.com</c>.
/// </summary>
public sealed class Email : ValueObject
{
    public const int MaxLength = 256;

    private Email(string value) => Value = value;

    public string Value { get; }

    public static Email Create(string? value)
    {
        var trimmed = value?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
        {
            throw new DomainException("E-mail address is required.");
        }

        if (trimmed.Length > MaxLength)
        {
            throw new DomainException(
                string.Create(CultureInfo.InvariantCulture, $"E-mail address must not exceed {MaxLength} characters."));
        }

        // Deliberately permissive: the only structural guarantee worth enforcing here is a single
        // "@" with something either side. Anything stricter rejects addresses that are legal in
        // practice, and delivery is what actually proves an address is real.
        var at = trimmed.IndexOf('@', StringComparison.Ordinal);
        if (at <= 0 || at != trimmed.LastIndexOf('@') || at == trimmed.Length - 1)
        {
            throw new DomainException($"'{trimmed}' is not a valid e-mail address.");
        }

        return new Email(trimmed.ToLowerInvariant());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
