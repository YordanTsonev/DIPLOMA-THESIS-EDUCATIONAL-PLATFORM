using EduPlatform.BuildingBlocks.Domain;

namespace EduPlatform.Modules.Identity.Domain;

/// <summary>
/// A person's given and family name.
/// </summary>
/// <remarks>
/// Bulgarian school records also carry a patronymic (бащино име), so it is modelled from the
/// start rather than bolted on later — adding a middle name after the gradebook and reports
/// exist would touch far more code.
/// </remarks>
public sealed class PersonName : ValueObject
{
    public const int MaxPartLength = 100;

    private PersonName(string first, string? middle, string last)
    {
        First = first;
        Middle = middle;
        Last = last;
    }

    public string First { get; }

    public string? Middle { get; }

    public string Last { get; }

    /// <summary>Display form, used in lists, the gradebook and notifications.</summary>
    public string Full => Middle is null ? $"{First} {Last}" : $"{First} {Middle} {Last}";

    public static PersonName Create(string? first, string? middle, string? last)
    {
        var firstPart = Require(first, nameof(first));
        var lastPart = Require(last, nameof(last));

        var middlePart = middle?.Trim();
        if (middlePart is { Length: 0 })
        {
            middlePart = null;
        }

        if (middlePart is { Length: > MaxPartLength })
        {
            throw new DomainException($"Middle name must not exceed {MaxPartLength} characters.");
        }

        return new PersonName(firstPart, middlePart, lastPart);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return First;
        yield return Middle;
        yield return Last;
    }

    public override string ToString() => Full;

    private static string Require(string? value, string part)
    {
        var trimmed = value?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
        {
            throw new DomainException($"The {part} name is required.");
        }

        return trimmed.Length > MaxPartLength
            ? throw new DomainException($"The {part} name must not exceed {MaxPartLength} characters.")
            : trimmed;
    }
}
