using EduPlatform.BuildingBlocks.Infrastructure.Persistence;
using EduPlatform.Modules.Notifications.Domain;
using Microsoft.EntityFrameworkCore;

namespace EduPlatform.Modules.Notifications.Infrastructure.Persistence;

public sealed class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options)
    : ModuleDbContext(options)
{
    public const string SchemaName = "notifications";

    public DbSet<Notification> Notifications => Set<Notification>();

    protected override string Schema => SchemaName;
}
