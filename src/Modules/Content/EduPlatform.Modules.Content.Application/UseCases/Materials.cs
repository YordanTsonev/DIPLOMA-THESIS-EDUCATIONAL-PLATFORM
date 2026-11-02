using EduPlatform.BuildingBlocks.Application.Messaging;
using EduPlatform.BuildingBlocks.Domain;
using EduPlatform.Modules.Content.Application.Abstractions;
using EduPlatform.Modules.Content.Contracts;
using EduPlatform.Modules.Content.Domain;
using EduPlatform.Modules.SchoolStructure.Contracts;
using FluentValidation;

namespace EduPlatform.Modules.Content.Application.UseCases;

public sealed record ListMaterialsQuery(Guid UserId, string Role, Guid? TeachingAssignmentId = null)
    : IQuery<MaterialsViewDto>;

public sealed record MaterialsViewDto(
    IReadOnlyList<TeachingAssignmentDto> Assignments,
    IReadOnlyList<MaterialFolderDto> Folders,
    IReadOnlyList<MaterialDto> Materials);

internal sealed class ListMaterialsHandler(
    IContentReadStore store,
    ISchoolDirectory school) : IQueryHandler<ListMaterialsQuery, MaterialsViewDto>
{
    public async Task<MaterialsViewDto> HandleAsync(ListMaterialsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var assignments = await school
            .ListAssignmentsForUserAsync(query.UserId, query.Role, cancellationToken)
            .ConfigureAwait(false);

        if (query.TeachingAssignmentId is { } requested)
        {
            assignments = assignments.Where(item => item.Id == requested).ToList();
        }

        if (assignments.Count == 0)
        {
            return new MaterialsViewDto([], [], []);
        }

        var ids = assignments.Select(item => item.Id).ToList();

        var folders = await store.ListFoldersAsync(ids, cancellationToken).ConfigureAwait(false);
        var materials = await store.ListMaterialsAsync(ids, cancellationToken).ConfigureAwait(false);

        return new MaterialsViewDto(assignments, folders, materials);
    }
}

public sealed record UploadMaterialCommand(
    Guid TeachingAssignmentId,
    Guid? FolderId,
    string? Title,
    string? Description,
    Stream Content,
    string FileName,
    string ContentType,
    long SizeBytes,
    Guid UploadedByUserId) : ICommand<MaterialDto>;

internal sealed class UploadMaterialHandler(
    IContentRepository repository,
    IContentReadStore store,
    IFileStorage storage,
    ISchoolDirectory school) : ICommandHandler<UploadMaterialCommand, MaterialDto>
{
    public async Task<MaterialDto> HandleAsync(UploadMaterialCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        await EnsureTeachesAsync(school, command.UploadedByUserId, command.TeachingAssignmentId, cancellationToken)
            .ConfigureAwait(false);

        var storageKey = await storage
            .SaveAsync(command.Content, command.FileName, command.ContentType, command.SizeBytes, cancellationToken)
            .ConfigureAwait(false);

        var material = Material.CreateFile(
            command.TeachingAssignmentId,
            command.FolderId,
            command.Title,
            command.Description,
            storageKey,
            command.FileName,
            command.ContentType,
            command.SizeBytes,
            command.UploadedByUserId);

        repository.AddMaterial(material);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return await store.FindMaterialAsync(material.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException("The material could not be read back.");
    }

    internal static async Task EnsureTeachesAsync(
        ISchoolDirectory school,
        Guid teacherUserId,
        Guid teachingAssignmentId,
        CancellationToken cancellationToken)
    {
        var assignments = await school
            .ListAssignmentsForUserAsync(teacherUserId, "Teacher", cancellationToken)
            .ConfigureAwait(false);

        if (!assignments.Any(item => item.Id == teachingAssignmentId))
        {
            throw new DomainException("You do not teach this subject to this class.");
        }
    }
}

public sealed record AddLinkCommand(
    Guid TeachingAssignmentId,
    Guid? FolderId,
    string? Title,
    string? Description,
    string Url,
    Guid UploadedByUserId) : ICommand<MaterialDto>;

internal sealed class AddLinkValidator : AbstractValidator<AddLinkCommand>
{
    public AddLinkValidator()
    {
        RuleFor(command => command.TeachingAssignmentId).NotEmpty();
        RuleFor(command => command.Url).NotEmpty().MaximumLength(Material.MaxUrlLength);
    }
}

internal sealed class AddLinkHandler(
    IContentRepository repository,
    IContentReadStore store,
    ISchoolDirectory school) : ICommandHandler<AddLinkCommand, MaterialDto>
{
    public async Task<MaterialDto> HandleAsync(AddLinkCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        await UploadMaterialHandler
            .EnsureTeachesAsync(school, command.UploadedByUserId, command.TeachingAssignmentId, cancellationToken)
            .ConfigureAwait(false);

        var material = Material.CreateLink(
            command.TeachingAssignmentId,
            command.FolderId,
            command.Title,
            command.Description,
            command.Url,
            command.UploadedByUserId);

        repository.AddMaterial(material);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return await store.FindMaterialAsync(material.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException("The material could not be read back.");
    }
}

public sealed record CreateFolderCommand(Guid TeachingAssignmentId, string Name, Guid TeacherUserId)
    : ICommand<MaterialFolderDto>;

internal sealed class CreateFolderValidator : AbstractValidator<CreateFolderCommand>
{
    public CreateFolderValidator()
    {
        RuleFor(command => command.TeachingAssignmentId).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(MaterialFolder.MaxNameLength);
    }
}

internal sealed class CreateFolderHandler(
    IContentRepository repository,
    ISchoolDirectory school) : ICommandHandler<CreateFolderCommand, MaterialFolderDto>
{
    public async Task<MaterialFolderDto> HandleAsync(CreateFolderCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        await UploadMaterialHandler
            .EnsureTeachesAsync(school, command.TeacherUserId, command.TeachingAssignmentId, cancellationToken)
            .ConfigureAwait(false);

        var folder = MaterialFolder.Create(command.TeachingAssignmentId, command.Name);
        repository.AddFolder(folder);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new MaterialFolderDto(folder.Id, folder.TeachingAssignmentId, folder.Name);
    }
}

public sealed record DeleteMaterialCommand(Guid MaterialId, Guid TeacherUserId) : ICommand;

internal sealed class DeleteMaterialHandler(
    IContentRepository repository,
    IFileStorage storage,
    ISchoolDirectory school) : CommandHandler<DeleteMaterialCommand>
{
    protected override async Task HandleCoreAsync(
        DeleteMaterialCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var material = await repository.FindMaterialAsync(command.MaterialId, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException("Material not found.");

        await UploadMaterialHandler
            .EnsureTeachesAsync(school, command.TeacherUserId, material.TeachingAssignmentId, cancellationToken)
            .ConfigureAwait(false);

        if (material.StorageKey is { } key)
        {
            await storage.DeleteAsync(key, cancellationToken).ConfigureAwait(false);
        }

        repository.RemoveMaterial(material);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

public sealed record DownloadMaterialQuery(Guid MaterialId, Guid UserId, string Role)
    : IQuery<MaterialDownloadDto>;

public sealed record MaterialDownloadDto(Stream Content, string FileName, string ContentType);

internal sealed class DownloadMaterialHandler(
    IContentRepository repository,
    IFileStorage storage,
    ISchoolDirectory school) : IQueryHandler<DownloadMaterialQuery, MaterialDownloadDto>
{
    public async Task<MaterialDownloadDto> HandleAsync(
        DownloadMaterialQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var material = await repository.FindMaterialAsync(query.MaterialId, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException("Material not found.");

        var assignments = await school
            .ListAssignmentsForUserAsync(query.UserId, query.Role, cancellationToken)
            .ConfigureAwait(false);

        if (!assignments.Any(item => item.Id == material.TeachingAssignmentId))
        {
            throw new DomainException("This material does not belong to your subjects.");
        }

        if (material.StorageKey is not { } key)
        {
            throw new DomainException("This material is a link, not a file.");
        }

        var content = await storage.OpenAsync(key, cancellationToken).ConfigureAwait(false);

        return new MaterialDownloadDto(
            content,
            material.FileName ?? "material",
            material.ContentType ?? "application/octet-stream");
    }
}
