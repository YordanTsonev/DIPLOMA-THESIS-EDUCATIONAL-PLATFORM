using System.Globalization;
using EduPlatform.Api.Authorization;
using EduPlatform.BuildingBlocks.Application.Messaging;
using EduPlatform.Modules.Homework.Application.UseCases;
using EduPlatform.Modules.Homework.Contracts;
using EduPlatform.Modules.Identity.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace EduPlatform.Api.Endpoints;

internal static class HomeworkEndpoints
{
    public static IEndpointRouteBuilder MapHomeworkEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/homework")
            .WithTags("Homework")
            .RequireAuthorization();

        group.MapGet("/", async (
                ICurrentUser currentUser,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is not { } userId)
                {
                    return Results.Unauthorized();
                }

                return Results.Ok(await dispatcher.QueryAsync(
                    new ListAssignmentsForTeacherQuery(userId), cancellationToken));
            })
            .RequireAuthorization(AuthorizationPolicies.TeacherOrAdmin)
            .WithName("ListHomeworkAssignments")
            .Produces<IReadOnlyList<AssignmentDto>>();

        group.MapGet("/me", async (
                ICurrentUser currentUser,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is not { } userId)
                {
                    return Results.Unauthorized();
                }

                return Results.Ok(await dispatcher.QueryAsync(
                    new ListMyHomeworkQuery(userId), cancellationToken));
            })
            .WithName("ListMyHomework")
            .Produces<IReadOnlyList<StudentAssignmentDto>>();

        group.MapGet("/students/{studentUserId:guid}", async (
                Guid studentUserId,
                ICurrentUser currentUser,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is not { } userId)
                {
                    return Results.Unauthorized();
                }

                return Results.Ok(await dispatcher.QueryAsync(
                    new ListHomeworkOfChildQuery(studentUserId, userId), cancellationToken));
            })
            .RequireAuthorization(AuthorizationPolicies.ParentOnly)
            .WithName("ListHomeworkOfChild")
            .Produces<IReadOnlyList<StudentAssignmentDto>>();

        group.MapGet("/{id:guid}/submissions", async (
                Guid id,
                ICurrentUser currentUser,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is not { } userId)
                {
                    return Results.Unauthorized();
                }

                return Results.Ok(await dispatcher.QueryAsync(
                    new ListSubmissionsQuery(id, userId), cancellationToken));
            })
            .RequireAuthorization(AuthorizationPolicies.TeacherOrAdmin)
            .WithName("ListHomeworkSubmissions")
            .Produces<IReadOnlyList<SubmissionDto>>();

        group.MapPost("/", async (
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

                if (!Guid.TryParse(form["teachingAssignmentId"], out var teachingAssignmentId))
                {
                    return Results.BadRequest("A teaching assignment is required.");
                }

                if (!DateOnly.TryParse(form["dueOn"], CultureInfo.InvariantCulture, out var dueOn))
                {
                    return Results.BadRequest("A deadline is required.");
                }

                var file = form.Files.GetFile("file");
                await using var content = file is { Length: > 0 } ? file.OpenReadStream() : null;

                var created = await dispatcher.SendAsync(
                    new CreateAssignmentCommand(
                        teachingAssignmentId,
                        form["title"].ToString(),
                        form["description"].ToString(),
                        dueOn,
                        form["isGraded"].ToString() is "true" or "True",
                        userId,
                        content,
                        file?.FileName,
                        file?.ContentType,
                        file?.Length ?? 0),
                    cancellationToken);

                return Results.Created($"/api/v1/homework/{created.Id}", created);
            })
            .RequireAuthorization(AuthorizationPolicies.TeacherOrAdmin)
            .DisableAntiforgery()
            .WithName("CreateHomeworkAssignment")
            .Produces<AssignmentDto>(StatusCodes.Status201Created);

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

                await dispatcher.SendAsync(new DeleteAssignmentCommand(id, userId), cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization(AuthorizationPolicies.TeacherOrAdmin)
            .WithName("DeleteHomeworkAssignment")
            .Produces(StatusCodes.Status204NoContent);

        group.MapPost("/{id:guid}/submissions", async (
                Guid id,
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
                await using var content = file is { Length: > 0 } ? file.OpenReadStream() : null;

                var created = await dispatcher.SendAsync(
                    new SubmitHomeworkCommand(
                        id,
                        userId,
                        form["text"].ToString(),
                        content,
                        file?.FileName,
                        file?.ContentType,
                        file?.Length ?? 0),
                    cancellationToken);

                return Results.Ok(created);
            })
            .DisableAntiforgery()
            .WithName("SubmitHomework")
            .Produces<SubmissionDto>();

        group.MapGet("/{id:guid}/file", async (
                Guid id,
                ICurrentUser currentUser,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is not { } userId || currentUser.Role is not { } role)
                {
                    return Results.Unauthorized();
                }

                var file = await dispatcher.QueryAsync(
                    new DownloadAssignmentFileQuery(id, userId, role.ToString()), cancellationToken);

                return Results.File(file.Content, file.ContentType, file.FileName);
            })
            .WithName("DownloadHomeworkFile")
            .Produces<FileResult>();

        group.MapGet("/submissions/{id:guid}/file", async (
                Guid id,
                ICurrentUser currentUser,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is not { } userId || currentUser.Role is not { } role)
                {
                    return Results.Unauthorized();
                }

                var file = await dispatcher.QueryAsync(
                    new DownloadSubmissionFileQuery(id, userId, role.ToString()), cancellationToken);

                return Results.File(file.Content, file.ContentType, file.FileName);
            })
            .WithName("DownloadSubmissionFile")
            .Produces<FileResult>();

        group.MapPut("/submissions/{id:guid}/review", async (
                Guid id,
                ReviewSubmissionRequest request,
                ICurrentUser currentUser,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is not { } userId)
                {
                    return Results.Unauthorized();
                }

                var reviewed = await dispatcher.SendAsync(
                    new ReviewSubmissionCommand(id, request.Mark, request.Feedback, userId), cancellationToken);

                return Results.Ok(reviewed);
            })
            .RequireAuthorization(AuthorizationPolicies.TeacherOrAdmin)
            .WithName("ReviewHomeworkSubmission")
            .Produces<SubmissionDto>();

        return endpoints;
    }
}

internal sealed record ReviewSubmissionRequest(decimal? Mark, string? Feedback);
