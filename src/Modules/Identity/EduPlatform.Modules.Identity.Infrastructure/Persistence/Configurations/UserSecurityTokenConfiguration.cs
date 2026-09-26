using EduPlatform.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduPlatform.Modules.Identity.Infrastructure.Persistence.Configurations;

internal sealed class UserSecurityTokenConfiguration : IEntityTypeConfiguration<UserSecurityToken>
{
    /// <summary>SHA-256 rendered as hex.</summary>
    private const int HashLength = 64;

    public void Configure(EntityTypeBuilder<UserSecurityToken> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("user_tokens");
        builder.HasKey(token => token.Id);

        builder.Property(token => token.TokenHash).HasMaxLength(HashLength).IsRequired();
        builder.Property(token => token.Purpose).HasConversion<int>().IsRequired();
        builder.Property(token => token.CreatedAt).IsRequired();
        builder.Property(token => token.ExpiresAt).IsRequired();

        // Confirming a mail link is a lookup by purpose and hash, so index the pair.
        builder.HasIndex(token => new { token.Purpose, token.TokenHash }).IsUnique();
        builder.HasIndex(token => token.UserId);
    }
}
