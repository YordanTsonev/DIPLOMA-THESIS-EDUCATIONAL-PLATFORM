using EduPlatform.Modules.Content.Application.Abstractions;
using EduPlatform.Modules.Content.Contracts;
using EduPlatform.Modules.Content.Domain;
using Microsoft.EntityFrameworkCore;

namespace EduPlatform.Modules.Content.Infrastructure.Persistence;

internal sealed class ContentRepository(ContentDbContext dbContext) : IContentRepository
{
    public Task<Material?> FindMaterialAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Materials.SingleOrDefaultAsync(material => material.Id == id, cancellationToken);

    public Task<MaterialFolder?> FindFolderAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.MaterialFolders.SingleOrDefaultAsync(folder => folder.Id == id, cancellationToken);

    public void AddMaterial(Material material) => dbContext.Materials.Add(material);

    public void RemoveMaterial(Material material) => dbContext.Materials.Remove(material);

    public void AddFolder(MaterialFolder folder) => dbContext.MaterialFolders.Add(folder);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}

internal sealed class ContentReadStore(ContentDbContext dbContext) : IContentReadStore
{
    public async Task<IReadOnlyList<MaterialDto>> ListMaterialsAsync(
        IReadOnlyCollection<Guid> teachingAssignmentIds,
        CancellationToken cancellationToken = default) =>
        await dbContext.Materials
            .AsNoTracking()
            .Where(material => teachingAssignmentIds.Contains(material.TeachingAssignmentId))
            .OrderByDescending(material => material.CreatedAt)
            .Select(material => new MaterialDto(
                material.Id,
                material.TeachingAssignmentId,
                material.FolderId,
                material.Kind.ToString(),
                material.Title,
                material.Description,
                material.FileName,
                material.ContentType,
                material.SizeBytes,
                material.Url,
                material.CreatedAt))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<MaterialFolderDto>> ListFoldersAsync(
        IReadOnlyCollection<Guid> teachingAssignmentIds,
        CancellationToken cancellationToken = default) =>
        await dbContext.MaterialFolders
            .AsNoTracking()
            .Where(folder => teachingAssignmentIds.Contains(folder.TeachingAssignmentId))
            .OrderBy(folder => folder.Name)
            .Select(folder => new MaterialFolderDto(folder.Id, folder.TeachingAssignmentId, folder.Name))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<MaterialDto?> FindMaterialAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Materials
            .AsNoTracking()
            .Where(material => material.Id == id)
            .Select(material => new MaterialDto(
                material.Id,
                material.TeachingAssignmentId,
                material.FolderId,
                material.Kind.ToString(),
                material.Title,
                material.Description,
                material.FileName,
                material.ContentType,
                material.SizeBytes,
                material.Url,
                material.CreatedAt))
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
}
