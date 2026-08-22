using EduPlatform.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduPlatform.Modules.Identity.Infrastructure.Persistence.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    /// <summary>SHA-256 rendered as hex.</summary>
    private const int HashLength = 64;

    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("refresh_tokens");
        builder.HasKey(token => token.Id);

        builder.Property(token => token.TokenHash).HasMaxLength(HashLength).IsRequired();
        builder.Property(token => token.ReplacedByTokenHash).HasMaxLength(HashLength);
        builder.Property(token => token.RevokedReason).HasMaxLength(200);
        builder.Property(token => token.DeviceInfo).HasMaxLength(400);
        builder.Property(token => token.CreatedAt).IsRequired();
        builder.Property(token => token.ExpiresAt).IsRequired();

        // Every refresh request is a lookup by hash, so this index is on the hot path.
        builder.HasIndex(token => token.TokenHash).IsUnique();
        builder.HasIndex(token => token.UserId);
    }
}
