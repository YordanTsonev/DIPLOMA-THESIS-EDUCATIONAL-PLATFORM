using EduPlatform.Modules.Notifications.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduPlatform.Modules.Notifications.Infrastructure.Persistence.Configurations;

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("notifications");
        builder.HasKey(notification => notification.Id);
        builder.Ignore(notification => notification.DomainEvents);

        builder.Property(notification => notification.RecipientUserId).IsRequired();
        builder.Property(notification => notification.Kind).HasConversion<string>().HasMaxLength(32);
        builder.Property(notification => notification.Title).HasMaxLength(Notification.MaxTitleLength).IsRequired();
        builder.Property(notification => notification.Body).HasMaxLength(Notification.MaxBodyLength);
        builder.Property(notification => notification.Link).HasMaxLength(Notification.MaxLinkLength);
        builder.Property(notification => notification.CreatedAt).IsRequired();
        builder.Property(notification => notification.ReadAt);

        builder.HasIndex(notification => new { notification.RecipientUserId, notification.CreatedAt });
    }
}
