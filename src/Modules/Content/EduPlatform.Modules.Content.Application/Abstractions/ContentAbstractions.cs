using EduPlatform.Modules.Content.Contracts;
using EduPlatform.Modules.Content.Domain;

namespace EduPlatform.Modules.Content.Application.Abstractions;

public interface IFileStorage
{
    Task<string> SaveAsync(
        Stream content,
        string fileName,
        string contentType,
        long sizeBytes,
        CancellationToken cancellationToken = default);

    Task<Stream> OpenAsync(string storageKey, CancellationToken cancellationToken = default);

    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}

public interface IContentRepository
{
    Task<Material?> FindMaterialAsync(Guid id, CancellationToken cancellationToken = default);

    Task<MaterialFolder?> FindFolderAsync(Guid id, CancellationToken cancellationToken = default);

    void AddMaterial(Material material);

    void RemoveMaterial(Material material);

    void AddFolder(MaterialFolder folder);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IContentReadStore
{
    Task<IReadOnlyList<MaterialDto>> ListMaterialsAsync(
        IReadOnlyCollection<Guid> teachingAssignmentIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MaterialFolderDto>> ListFoldersAsync(
        IReadOnlyCollection<Guid> teachingAssignmentIds,
        CancellationToken cancellationToken = default);

    Task<MaterialDto?> FindMaterialAsync(Guid id, CancellationToken cancellationToken = default);
}
