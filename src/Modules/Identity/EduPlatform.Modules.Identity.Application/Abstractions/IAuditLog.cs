using EduPlatform.Modules.Identity.Domain;

namespace EduPlatform.Modules.Identity.Application.Abstractions;

/// <summary>Appends entries to the security audit trail.</summary>
/// <remarks>
/// Writes are flushed with the surrounding unit of work, so an audited action and its audit row
/// commit together: an action that rolls back leaves no entry claiming it happened.
/// </remarks>
public interface IAuditLog
{
    void Record(AuditEntry entry);
}
