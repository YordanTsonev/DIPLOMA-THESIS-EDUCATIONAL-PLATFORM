using EduPlatform.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduPlatform.Modules.Identity.Infrastructure.Persistence.Configurations;

internal sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("audit_log");
        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.Action).HasMaxLength(64).IsRequired();
        builder.Property(entry => entry.Detail).HasMaxLength(500);
        builder.Property(entry => entry.IpAddress).HasMaxLength(64);
        builder.Property(entry => entry.OccurredAt).IsRequired();

        // No foreign keys to users on purpose. The log has to survive a user row being removed,
        // and a failed sign-in against an unknown address has no user to point at.
        builder.HasIndex(entry => entry.OccurredAt).IsDescending();
        builder.HasIndex(entry => entry.SubjectUserId);
        builder.HasIndex(entry => entry.Action);
    }
}
