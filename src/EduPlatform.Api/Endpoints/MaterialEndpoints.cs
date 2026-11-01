using EduPlatform.Api.Authorization;
using EduPlatform.BuildingBlocks.Application.Messaging;
using EduPlatform.Modules.Content.Application.UseCases;
using EduPlatform.Modules.Content.Contracts;
using EduPlatform.Modules.Content.Domain;
using EduPlatform.Modules.Identity.Application.Abstractions;

namespace EduPlatform.Api.Endpoints;

internal static class MaterialEndpoints
{
    public static IEndpointRouteBuilder MapMaterialEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/materials")
            .WithTags("Materials")
            .RequireAuthorization();

        group.MapGet("/", async (
                Guid? teachingAssignmentId,
                ICurrentUser currentUser,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is not { } userId || currentUser.Role is not { } role)
                {
                    return Results.Unauthorized();
                }

                var view = await dispatcher.QueryAsync(
                    new ListMaterialsQuery(userId, role.ToString(), teachingAssignmentId), cancellationToken);

                return Results.Ok(view);
            })
            .WithName("ListMaterials")
            .Produces<MaterialsViewDto>();

        group.MapPost("/upload", async (
                HttpRequest request,
                ICurrentUser currentUser,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is not { } userId)
                {
                    return Results.Unauthorized();
                }

                if (!request.HasFormContentType)
                {
                    return Results.BadRequest("Expected a multipart form.");
                }

                var form = await request.ReadFormAsync(cancellationToken);
                var file = form.Files.GetFile("file");

                if (file is null || file.Length == 0)
                {
                    return Results.BadRequest("A file is required.");
                }

                if (!Guid.TryParse(form["teachingAssignmentId"], out var assignmentId))
                {
                    return Results.BadRequest("A teaching assignment is required.");
                }

                Guid? folderId = Guid.TryParse(form["folderId"], out var parsedFolder) ? parsedFolder : null;

                await using var content = file.OpenReadStream();

                var created = await dispatcher.SendAsync(
                    new UploadMaterialCommand(
                        assignmentId,
                        folderId,
                        form["title"],
                        form["description"],
                        content,
                        file.FileName,
                        file.ContentType,
                        file.Length,
                        userId),
                    cancellationToken);

                return Results.Created($"/api/v1/materials/{created.Id}", created);
            })
            .RequireAuthorization(AuthorizationPolicies.TeacherOrAdmin)
            .DisableAntiforgery()
            .WithName("UploadMaterial")
            .Produces<MaterialDto>(StatusCodes.Status201Created);

        group.MapPost("/links", async (
                AddLinkRequest request,
                ICurrentUser currentUser,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is not { } userId)
                {
                    return Results.Unauthorized();
                }

                var created = await dispatcher.SendAsync(
                    new AddLinkCommand(
                        request.TeachingAssignmentId,
                        request.FolderId,
                        request.Title,
                        request.Description,
                        request.Url,
                        userId),
                    cancellationToken);

                return Results.Created($"/api/v1/materials/{created.Id}", created);
            })
            .RequireAuthorization(AuthorizationPolicies.TeacherOrAdmin)
            .WithName("AddMaterialLink")
            .Produces<MaterialDto>(StatusCodes.Status201Created);

        group.MapPost("/folders", async (
                CreateFolderRequest request,
                ICurrentUser currentUser,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is not { } userId)
                {
                    return Results.Unauthorized();
                }

                var created = await dispatcher.SendAsync(
                    new CreateFolderCommand(request.TeachingAssignmentId, request.Name, userId), cancellationToken);

                return Results.Created($"/api/v1/materials/folders/{created.Id}", created);
            })
            .RequireAuthorization(AuthorizationPolicies.TeacherOrAdmin)
            .WithName("CreateMaterialFolder")
            .Produces<MaterialFolderDto>(StatusCodes.Status201Created);

        group.MapGet("/{id:guid}/download", async (
                Guid id,
                ICurrentUser currentUser,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is not { } userId || currentUser.Role is not { } role)
                {
                    return Results.Unauthorized();
                }

                var download = await dispatcher.QueryAsync(
                    new DownloadMaterialQuery(id, userId, role.ToString()), cancellationToken);

                return Results.File(download.Content, download.ContentType, download.FileName);
            })
            .WithName("DownloadMaterial");

        group.MapDelete("/{id:guid}", async (
                Guid id,
                ICurrentUser currentUser,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is not { } userId)
                {
                    return Results.Unauthorized();
                }

                await dispatcher.SendAsync(new DeleteMaterialCommand(id, userId), cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization(AuthorizationPolicies.TeacherOrAdmin)
            .WithName("DeleteMaterial")
            .Produces(StatusCodes.Status204NoContent);

        group.MapGet("/limits", () => Results.Ok(new MaterialLimitsResponse(
                Material.MaxFileSizeBytes,
                Material.AllowedExtensions)))
            .WithName("GetMaterialLimits")
            .Produces<MaterialLimitsResponse>();

        return endpoints;
    }
}

internal sealed record AddLinkRequest(
    Guid TeachingAssignmentId,
    Guid? FolderId,
    string? Title,
    string? Description,
    string Url);

internal sealed record CreateFolderRequest(Guid TeachingAssignmentId, string Name);

internal sealed record MaterialLimitsResponse(long MaxFileSizeBytes, IReadOnlyList<string> AllowedExtensions);
