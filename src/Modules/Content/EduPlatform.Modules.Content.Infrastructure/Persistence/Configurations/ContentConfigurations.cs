using EduPlatform.Modules.Content.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduPlatform.Modules.Content.Infrastructure.Persistence.Configurations;

internal sealed class MaterialFolderConfiguration : IEntityTypeConfiguration<MaterialFolder>
{
    public void Configure(EntityTypeBuilder<MaterialFolder> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("material_folders");
        builder.HasKey(folder => folder.Id);

        builder.Property(folder => folder.TeachingAssignmentId).IsRequired();
        builder.Property(folder => folder.Name).HasMaxLength(MaterialFolder.MaxNameLength).IsRequired();
        builder.Property(folder => folder.CreatedAt).IsRequired();

        builder.Ignore(folder => folder.DomainEvents);
        builder.HasIndex(folder => folder.TeachingAssignmentId);
    }
}

internal sealed class MaterialConfiguration : IEntityTypeConfiguration<Material>
{
    public void Configure(EntityTypeBuilder<Material> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("materials");
        builder.HasKey(material => material.Id);

        builder.Property(material => material.TeachingAssignmentId).IsRequired();
        builder.Property(material => material.Kind).HasConversion<int>().IsRequired();
        builder.Property(material => material.Title).HasMaxLength(Material.MaxTitleLength).IsRequired();
        builder.Property(material => material.Description).HasMaxLength(Material.MaxDescriptionLength);
        builder.Property(material => material.StorageKey).HasMaxLength(400);
        builder.Property(material => material.FileName).HasMaxLength(Material.MaxFileNameLength);
        builder.Property(material => material.ContentType).HasMaxLength(160);
        builder.Property(material => material.Url).HasMaxLength(Material.MaxUrlLength);
        builder.Property(material => material.UploadedByUserId).IsRequired();
        builder.Property(material => material.CreatedAt).IsRequired();

        builder.Ignore(material => material.DomainEvents);

        builder.HasIndex(material => material.TeachingAssignmentId);
        builder.HasIndex(material => material.FolderId);

        builder.HasOne<MaterialFolder>()
            .WithMany()
            .HasForeignKey(material => material.FolderId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
