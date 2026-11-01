using EduPlatform.Api.Authorization;
using EduPlatform.BuildingBlocks.Application.Messaging;
using EduPlatform.Modules.Gradebook.Application.UseCases;
using EduPlatform.Modules.Gradebook.Contracts;
using EduPlatform.Modules.Gradebook.Domain;
using EduPlatform.Modules.Identity.Application.Abstractions;

namespace EduPlatform.Api.Endpoints;

internal static class GradebookEndpoints
{
    public static IEndpointRouteBuilder MapGradebookEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/gradebook")
            .WithTags("Gradebook")
            .RequireAuthorization();

        group.MapGet("/class/{teachingAssignmentId:guid}", async (
                Guid teachingAssignmentId,
                ICurrentUser currentUser,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is not { } userId)
                {
                    return Results.Unauthorized();
                }

                var view = await dispatcher.QueryAsync(
                    new GetClassGradebookQuery(teachingAssignmentId, userId), cancellationToken);

                return Results.Ok(view);
            })
            .RequireAuthorization(AuthorizationPolicies.TeacherOrAdmin)
            .WithName("GetClassGradebook")
            .Produces<ClassGradebookDto>();

        group.MapGet("/me", async (
                ICurrentUser currentUser,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is not { } userId)
                {
                    return Results.Unauthorized();
                }

                var report = await dispatcher.QueryAsync(new GetStudentReportQuery(userId), cancellationToken);
                return Results.Ok(report);
            })
            .WithName("GetMyGrades")
            .Produces<StudentReportDto>();

        group.MapGet("/students/{studentUserId:guid}", async (
                Guid studentUserId,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
                Results.Ok(await dispatcher.QueryAsync(
                    new GetStudentReportQuery(studentUserId), cancellationToken)))
            .RequireAuthorization(AuthorizationPolicies.TeacherOrAdmin)
            .WithName("GetStudentReport")
            .Produces<StudentReportDto>();

        group.MapPost("/grades", async (
                GiveGradeRequest request,
                ICurrentUser currentUser,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is not { } userId)
                {
                    return Results.Unauthorized();
                }

                var created = await dispatcher.SendAsync(
                    new GiveGradeCommand(
                        request.StudentUserId,
                        request.TeachingAssignmentId,
                        request.Value,
                        request.Kind,
                        request.Reason,
                        request.Comment,
                        userId),
                    cancellationToken);

                return Results.Created($"/api/v1/gradebook/grades/{created.Id}", created);
            })
            .RequireAuthorization(AuthorizationPolicies.TeacherOrAdmin)
            .WithName("GiveGrade")
            .Produces<GradeDto>(StatusCodes.Status201Created);

        group.MapDelete("/grades/{id:guid}", async (
                Guid id,
                ICurrentUser currentUser,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is not { } userId)
                {
                    return Results.Unauthorized();
                }

                await dispatcher.SendAsync(new DeleteGradeCommand(id, userId), cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization(AuthorizationPolicies.TeacherOrAdmin)
            .WithName("DeleteGrade")
            .Produces(StatusCodes.Status204NoContent);

        group.MapPost("/absences", async (
                RecordAbsenceRequest request,
                ICurrentUser currentUser,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is not { } userId)
                {
                    return Results.Unauthorized();
                }

                var created = await dispatcher.SendAsync(
                    new RecordAbsenceCommand(
                        request.StudentUserId,
                        request.TeachingAssignmentId,
                        request.Date,
                        request.Period,
                        request.Kind,
                        request.Note,
                        userId),
                    cancellationToken);

                return Results.Created($"/api/v1/gradebook/absences/{created.Id}", created);
            })
            .RequireAuthorization(AuthorizationPolicies.TeacherOrAdmin)
            .WithName("RecordAbsence")
            .Produces<AbsenceDto>(StatusCodes.Status201Created);

        group.MapPut("/absences/{id:guid}/excuse", async (
                Guid id,
                ExcuseAbsenceRequest request,
                ICurrentUser currentUser,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is not { } userId)
                {
                    return Results.Unauthorized();
                }

                var updated = await dispatcher.SendAsync(
                    new ExcuseAbsenceCommand(id, request.Note, userId), cancellationToken);

                return Results.Ok(updated);
            })
            .RequireAuthorization(AuthorizationPolicies.TeacherOrAdmin)
            .WithName("ExcuseAbsence")
            .Produces<AbsenceDto>();

        return endpoints;
    }
}

internal sealed record GiveGradeRequest(
    Guid StudentUserId,
    Guid TeachingAssignmentId,
    decimal Value,
    GradeKind Kind,
    GradeReason Reason,
    string? Comment);

internal sealed record RecordAbsenceRequest(
    Guid StudentUserId,
    Guid TeachingAssignmentId,
    DateOnly Date,
    int Period,
    AbsenceKind Kind,
    string? Note);

internal sealed record ExcuseAbsenceRequest(string? Note);
