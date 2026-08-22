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

        // Email is a value object but maps to a single column: it is the login identifier and
        // needs a unique index, which a separate owned table would make awkward.
        builder.Property(user => user.Email)
            .HasConversion(email => email.Value, value => Email.Create(value))
            .HasColumnName("email")
            .HasMaxLength(Email.MaxLength)
            .IsRequired();

        builder.HasIndex(user => user.Email).IsUnique();

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

        builder.Navigation(user => user.RefreshTokens).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(user => user.DomainEvents);
    }
}
