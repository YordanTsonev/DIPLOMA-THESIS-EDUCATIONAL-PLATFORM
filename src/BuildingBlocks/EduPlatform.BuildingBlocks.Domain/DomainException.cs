namespace EduPlatform.BuildingBlocks.Domain;

/// <summary>
/// Thrown when an operation would leave the domain in an invalid state
/// (for example a grade outside the 2.00–6.00 range).
/// Surfaces to the caller as HTTP 409 Conflict, not 500.
/// </summary>
public class DomainException(string message) : Exception(message);
