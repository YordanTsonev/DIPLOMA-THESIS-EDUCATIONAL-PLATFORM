using EduPlatform.Api.Authorization;
using EduPlatform.Modules.Identity.Application.Abstractions;
using EduPlatform.BuildingBlocks.Application.Messaging;
using EduPlatform.Modules.SchoolStructure.Application.UseCases;
using EduPlatform.Modules.SchoolStructure.Contracts;

namespace EduPlatform.Api.Endpoints;

internal static class SchoolEndpoints
{
    public static IEndpointRouteBuilder MapSchoolEndpoints(this IEndpointRouteBuilder endpoints)
    {
        MapAcademicYears(endpoints);
        MapSubjects(endpoints);
        MapClasses(endpoints);
        MapAssignments(endpoints);
        MapSchedule(endpoints);
        return endpoints;
    }

    private static void MapAcademicYears(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/academic-years")
            .WithTags("School structure")
            .RequireAuthorization();

        group.MapGet("/", async (IDispatcher dispatcher, CancellationToken cancellationToken) =>
                Results.Ok(await dispatcher.QueryAsync(new ListAcademicYearsQuery(), cancellationToken)))
            .WithName("ListAcademicYears")
            .Produces<IReadOnlyList<AcademicYearDto>>();

        group.MapPost("/", async (
                CreateAcademicYearRequest request,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                var created = await dispatcher.SendAsync(
                    new CreateAcademicYearCommand(request.Name, request.StartsOn, request.EndsOn, request.MakeCurrent),
                    cancellationToken);

                return Results.Created($"/api/v1/academic-years/{created.Id}", created);
            })
            .RequireAuthorization(AuthorizationPolicies.AdminOnly)
            .WithName("CreateAcademicYear")
            .Produces<AcademicYearDto>(StatusCodes.Status201Created);
    }

    private static void MapSubjects(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/subjects")
            .WithTags("School structure")
            .RequireAuthorization();

        group.MapGet("/", async (
                bool? includeInactive,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
                Results.Ok(await dispatcher.QueryAsync(
                    new ListSubjectsQuery(includeInactive ?? false), cancellationToken)))
            .WithName("ListSubjects")
            .Produces<IReadOnlyList<SubjectDto>>();

        group.MapPost("/", async (
                CreateSubjectRequest request,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                var created = await dispatcher.SendAsync(
                    new CreateSubjectCommand(request.Name, request.Code), cancellationToken);

                return Results.Created($"/api/v1/subjects/{created.Id}", created);
            })
            .RequireAuthorization(AuthorizationPolicies.AdminOnly)
            .WithName("CreateSubject")
            .Produces<SubjectDto>(StatusCodes.Status201Created);

        group.MapPut("/{id:guid}/active", async (
                Guid id,
                SetActiveRequest request,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
                Results.Ok(await dispatcher.SendAsync(
                    new SetSubjectActiveCommand(id, request.IsActive), cancellationToken)))
            .RequireAuthorization(AuthorizationPolicies.AdminOnly)
            .WithName("SetSubjectActive")
            .Produces<SubjectDto>();
    }

    private static void MapClasses(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/classes")
            .WithTags("School structure")
            .RequireAuthorization();

        group.MapGet("/", async (
                Guid? academicYearId,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
                Results.Ok(await dispatcher.QueryAsync(new ListClassesQuery(academicYearId), cancellationToken)))
            .WithName("ListClasses")
            .Produces<IReadOnlyList<SchoolClassDto>>();

        group.MapGet("/{id:guid}/students", async (
                Guid id,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
                Results.Ok(await dispatcher.QueryAsync(new GetClassStudentsQuery(id), cancellationToken)))
            .RequireAuthorization(AuthorizationPolicies.TeacherOrAdmin)
            .WithName("ListClassStudents")
            .Produces<IReadOnlyList<ClassStudentDto>>();

        group.MapPost("/", async (
                CreateClassRequest request,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                var created = await dispatcher.SendAsync(
                    new CreateClassCommand(
                        request.AcademicYearId, request.Grade, request.Section, request.HomeroomTeacherId),
                    cancellationToken);

                return Results.Created($"/api/v1/classes/{created.Id}", created);
            })
            .RequireAuthorization(AuthorizationPolicies.AdminOnly)
            .WithName("CreateClass")
            .Produces<SchoolClassDto>(StatusCodes.Status201Created);

        group.MapPost("/{id:guid}/students", async (
                Guid id,
                EnrollStudentRequest request,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                await dispatcher.SendAsync(new EnrollStudentCommand(id, request.StudentUserId), cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization(AuthorizationPolicies.AdminOnly)
            .WithName("EnrollStudent")
            .Produces(StatusCodes.Status204NoContent);

        group.MapDelete("/{id:guid}/students/{studentUserId:guid}", async (
                Guid id,
                Guid studentUserId,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                await dispatcher.SendAsync(new WithdrawStudentCommand(id, studentUserId), cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization(AuthorizationPolicies.AdminOnly)
            .WithName("WithdrawStudent")
            .Produces(StatusCodes.Status204NoContent);
    }

    private static void MapAssignments(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/teaching-assignments")
            .WithTags("School structure")
            .RequireAuthorization();

        group.MapGet("/", async (
                Guid? schoolClassId,
                Guid? teacherUserId,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
                Results.Ok(await dispatcher.QueryAsync(
                    new ListAssignmentsQuery(schoolClassId, teacherUserId), cancellationToken)))
            .WithName("ListTeachingAssignments")
            .Produces<IReadOnlyList<TeachingAssignmentDto>>();

        group.MapPost("/", async (
                CreateAssignmentRequest request,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                var created = await dispatcher.SendAsync(
                    new CreateAssignmentCommand(
                        request.TeacherUserId, request.SubjectId, request.SchoolClassId, request.WeeklyHours),
                    cancellationToken);

                return Results.Created($"/api/v1/teaching-assignments/{created.Id}", created);
            })
            .RequireAuthorization(AuthorizationPolicies.AdminOnly)
            .WithName("CreateTeachingAssignment")
            .Produces<TeachingAssignmentDto>(StatusCodes.Status201Created);
    }

    private static void MapSchedule(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/schedule")
            .WithTags("School structure")
            .RequireAuthorization();

        group.MapGet("/me", async (
                ICurrentUser currentUser,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is not { } userId || currentUser.Role is not { } role)
                {
                    return Results.Unauthorized();
                }

                var schedule = await dispatcher.QueryAsync(
                    new GetMyScheduleQuery(userId, role.ToString()), cancellationToken);

                return Results.Ok(schedule);
            })
            .WithName("GetMySchedule")
            .Produces<IReadOnlyList<ScheduleSlotDto>>();

        group.MapGet("/", async (
                Guid? schoolClassId,
                Guid? teacherUserId,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
                Results.Ok(await dispatcher.QueryAsync(
                    new GetScheduleQuery(schoolClassId, teacherUserId), cancellationToken)))
            .WithName("GetSchedule")
            .Produces<IReadOnlyList<ScheduleSlotDto>>();

        group.MapPost("/", async (
                AddScheduleSlotRequest request,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                var created = await dispatcher.SendAsync(
                    new AddScheduleSlotCommand(
                        request.TeachingAssignmentId, request.DayOfWeek, request.Period, request.Room),
                    cancellationToken);

                return Results.Created($"/api/v1/schedule/{created.Id}", created);
            })
            .RequireAuthorization(AuthorizationPolicies.AdminOnly)
            .WithName("AddScheduleSlot")
            .Produces<ScheduleSlotDto>(StatusCodes.Status201Created);

        group.MapDelete("/{id:guid}", async (
                Guid id,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                await dispatcher.SendAsync(new RemoveScheduleSlotCommand(id), cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization(AuthorizationPolicies.AdminOnly)
            .WithName("RemoveScheduleSlot")
            .Produces(StatusCodes.Status204NoContent);
    }
}

internal sealed record AddScheduleSlotRequest(
    Guid TeachingAssignmentId,
    DayOfWeek DayOfWeek,
    int Period,
    string? Room);

internal sealed record CreateAcademicYearRequest(string Name, DateOnly StartsOn, DateOnly EndsOn, bool MakeCurrent);

internal sealed record CreateSubjectRequest(string Name, string Code);

internal sealed record CreateClassRequest(Guid AcademicYearId, int Grade, string Section, Guid? HomeroomTeacherId);

internal sealed record EnrollStudentRequest(Guid StudentUserId);

internal sealed record CreateAssignmentRequest(
    Guid TeacherUserId,
    Guid SubjectId,
    Guid SchoolClassId,
    int WeeklyHours);
