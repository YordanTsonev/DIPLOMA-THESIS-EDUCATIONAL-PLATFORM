using EduPlatform.Modules.Identity.Application.Abstractions;
using EduPlatform.Modules.Identity.Domain;

namespace EduPlatform.Modules.Identity.Infrastructure.Persistence;

/// <summary>
/// Appends audit entries to the same unit of work as the action being audited, so the two
/// commit or roll back together.
/// </summary>
internal sealed class AuditLog(IdentityDbContext dbContext) : IAuditLog
{
    public void Record(AuditEntry entry) => dbContext.AuditEntries.Add(entry);
}
