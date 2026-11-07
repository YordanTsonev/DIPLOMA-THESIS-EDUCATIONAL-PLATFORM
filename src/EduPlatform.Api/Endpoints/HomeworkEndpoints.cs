using EduPlatform.Api.Authorization;
using EduPlatform.BuildingBlocks.Application.Messaging;
using EduPlatform.Modules.Homework.Application.UseCases;
using EduPlatform.Modules.Homework.Contracts;
using EduPlatform.Modules.Identity.Application.Abstractions;

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
                CreateHomeworkRequest request,
                ICurrentUser currentUser,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is not { } userId)
                {
                    return Results.Unauthorized();
                }

                var created = await dispatcher.SendAsync(
                    new CreateAssignmentCommand(
                        request.TeachingAssignmentId,
                        request.Title,
                        request.Description,
                        request.DueOn,
                        request.IsGraded,
                        userId),
                    cancellationToken);

                return Results.Created($"/api/v1/homework/{created.Id}", created);
            })
            .RequireAuthorization(AuthorizationPolicies.TeacherOrAdmin)
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
                SubmitHomeworkRequest request,
                ICurrentUser currentUser,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is not { } userId)
                {
                    return Results.Unauthorized();
                }

                var created = await dispatcher.SendAsync(
                    new SubmitHomeworkCommand(id, userId, request.Text), cancellationToken);

                return Results.Ok(created);
            })
            .WithName("SubmitHomework")
            .Produces<SubmissionDto>();

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

internal sealed record CreateHomeworkRequest(
    Guid TeachingAssignmentId,
    string Title,
    string? Description,
    DateOnly DueOn,
    bool IsGraded);

internal sealed record SubmitHomeworkRequest(string Text);

internal sealed record ReviewSubmissionRequest(decimal? Mark, string? Feedback);
