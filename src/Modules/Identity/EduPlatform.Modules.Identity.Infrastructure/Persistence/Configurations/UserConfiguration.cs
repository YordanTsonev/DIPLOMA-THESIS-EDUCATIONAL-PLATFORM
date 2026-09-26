using EduPlatform.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduPlatform.Modules.Identity.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("users");
        builder.HasKey(user => user.Id);

        builder.Property(user => user.EmailAddress)
            .HasColumnName("email")
            .HasMaxLength(Email.MaxLength)
            .IsRequired();

        // The value object is derived from the column, not stored separately.
        builder.Ignore(user => user.Email);

        // The login lookup and the uniqueness rule both run against this column.
        builder.HasIndex(user => user.EmailAddress)
            .HasDatabaseName("ix_users_email")
            .IsUnique();

        builder.ComplexProperty(user => user.Name, name =>
        {
            name.Property(part => part.First).HasColumnName("first_name").HasMaxLength(PersonName.MaxPartLength).IsRequired();
            name.Property(part => part.Middle).HasColumnName("middle_name").HasMaxLength(PersonName.MaxPartLength);
            name.Property(part => part.Last).HasColumnName("last_name").HasMaxLength(PersonName.MaxPartLength).IsRequired();
        });

        builder.Property(user => user.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(user => user.Role).HasConversion<int>().IsRequired();
        builder.Property(user => user.PhoneNumber).HasMaxLength(32);
        builder.Property(user => user.IsActive).IsRequired();
        builder.Property(user => user.EmailConfirmed).IsRequired();
        builder.Property(user => user.CreatedAt).IsRequired();

        // Sign-in screens list users by role, and every authorisation check filters on it.
        builder.HasIndex(user => user.Role);

        builder.HasMany(user => user.RefreshTokens)
            .WithOne()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(user => user.SecurityTokens)
            .WithOne()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(user => user.RefreshTokens).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(user => user.SecurityTokens).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(user => user.DomainEvents);
    }
}
