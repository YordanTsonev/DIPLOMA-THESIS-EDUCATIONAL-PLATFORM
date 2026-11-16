using EduPlatform.Api.Authorization;
using EduPlatform.BuildingBlocks.Application.Messaging;
using EduPlatform.Modules.Assessment.Application.UseCases;
using EduPlatform.Modules.Assessment.Contracts;
using EduPlatform.Modules.Assessment.Domain;
using EduPlatform.Modules.Identity.Application.Abstractions;

namespace EduPlatform.Api.Endpoints;

internal static class AssessmentEndpoints
{
    public static IEndpointRouteBuilder MapAssessmentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        MapQuestions(endpoints);
        MapExams(endpoints);
        MapAttempts(endpoints);
        return endpoints;
    }

    private static void MapQuestions(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/questions")
            .WithTags("Assessment")
            .RequireAuthorization(AuthorizationPolicies.TeacherOrAdmin);

        group.MapGet("/", async (
                Guid? subjectId,
                bool? ownOnly,
                ICurrentUser currentUser,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is not { } userId)
                {
                    return Results.Unauthorized();
                }

                return Results.Ok(await dispatcher.QueryAsync(
                    new ListQuestionsQuery(userId, subjectId, ownOnly ?? false), cancellationToken));
            })
            .WithName("ListQuestions")
            .Produces<IReadOnlyList<QuestionDto>>();

        group.MapPost("/", async (
                CreateQuestionRequest request,
                ICurrentUser currentUser,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is not { } userId)
                {
                    return Results.Unauthorized();
                }

                var created = await dispatcher.SendAsync(
                    new CreateQuestionCommand(
                        request.SubjectId,
                        request.Kind,
                        request.Text,
                        request.Explanation,
                        request.Options ?? [],
                        userId),
                    cancellationToken);

                return Results.Created($"/api/v1/questions/{created.Id}", created);
            })
            .WithName("CreateQuestion")
            .Produces<QuestionDto>(StatusCodes.Status201Created);

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

                await dispatcher.SendAsync(new DeleteQuestionCommand(id, userId), cancellationToken);
                return Results.NoContent();
            })
            .WithName("DeleteQuestion")
            .Produces(StatusCodes.Status204NoContent);
    }

    private static void MapExams(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/exams")
            .WithTags("Assessment")
            .RequireAuthorization(AuthorizationPolicies.TeacherOrAdmin);

        group.MapGet("/", async (
                ICurrentUser currentUser,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is not { } userId)
                {
                    return Results.Unauthorized();
                }

                return Results.Ok(await dispatcher.QueryAsync(new ListExamsQuery(userId), cancellationToken));
            })
            .WithName("ListExams")
            .Produces<IReadOnlyList<ExamDto>>();

        group.MapGet("/{id:guid}", async (
                Guid id,
                ICurrentUser currentUser,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is not { } userId)
                {
                    return Results.Unauthorized();
                }

                return Results.Ok(await dispatcher.QueryAsync(new GetExamQuery(id, userId), cancellationToken));
            })
            .WithName("GetExam")
            .Produces<ExamDto>();

        group.MapPost("/", async (
                CreateExamRequest request,
                ICurrentUser currentUser,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is not { } userId)
                {
                    return Results.Unauthorized();
                }

                var created = await dispatcher.SendAsync(
                    new CreateExamCommand(
                        request.TeachingAssignmentId,
                        request.Title,
                        request.Description,
                        request.DurationMinutes,
                        userId),
                    cancellationToken);

                return Results.Created($"/api/v1/exams/{created.Id}", created);
            })
            .WithName("CreateExam")
            .Produces<ExamDto>(StatusCodes.Status201Created);

        group.MapPost("/{id:guid}/items", async (
                Guid id,
                AddExamItemRequest request,
                ICurrentUser currentUser,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is not { } userId)
                {
                    return Results.Unauthorized();
                }

                return Results.Ok(await dispatcher.SendAsync(
                    new AddExamItemCommand(id, request.QuestionId, request.Points, userId), cancellationToken));
            })
            .WithName("AddExamItem")
            .Produces<ExamDto>();

        group.MapPost("/{id:guid}/questions", async (
                Guid id,
                AddExamQuestionRequest request,
                ICurrentUser currentUser,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is not { } userId)
                {
                    return Results.Unauthorized();
                }

                return Results.Ok(await dispatcher.SendAsync(
                    new AddExamQuestionCommand(
                        id,
                        request.Kind,
                        request.Text,
                        request.Explanation,
                        request.Options ?? [],
                        request.Points,
                        userId),
                    cancellationToken));
            })
            .WithName("AddExamQuestion")
            .Produces<ExamDto>();

        group.MapDelete("/{id:guid}/items/{questionId:guid}", async (
                Guid id,
                Guid questionId,
                ICurrentUser currentUser,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is not { } userId)
                {
                    return Results.Unauthorized();
                }

                return Results.Ok(await dispatcher.SendAsync(
                    new RemoveExamItemCommand(id, questionId, userId), cancellationToken));
            })
            .WithName("RemoveExamItem")
            .Produces<ExamDto>();

        group.MapPut("/{id:guid}/publish", async (
                Guid id,
                PublishExamRequest request,
                ICurrentUser currentUser,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is not { } userId)
                {
                    return Results.Unauthorized();
                }

                return Results.Ok(await dispatcher.SendAsync(
                    new PublishExamCommand(id, request.OpensAt, request.ClosesAt, userId), cancellationToken));
            })
            .WithName("PublishExam")
            .Produces<ExamDto>();

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

                await dispatcher.SendAsync(new DeleteExamCommand(id, userId), cancellationToken);
                return Results.NoContent();
            })
            .WithName("DeleteExam")
            .Produces(StatusCodes.Status204NoContent);

        group.MapGet("/{id:guid}/attempts", async (
                Guid id,
                ICurrentUser currentUser,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is not { } userId)
                {
                    return Results.Unauthorized();
                }

                return Results.Ok(await dispatcher.QueryAsync(new ListAttemptsQuery(id, userId), cancellationToken));
            })
            .WithName("ListExamAttempts")
            .Produces<IReadOnlyList<AttemptDto>>();
    }

    private static void MapAttempts(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/attempts")
            .WithTags("Assessment")
            .RequireAuthorization();

        group.MapGet("/me", async (
                ICurrentUser currentUser,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is not { } userId)
                {
                    return Results.Unauthorized();
                }

                return Results.Ok(await dispatcher.QueryAsync(new ListMyExamsQuery(userId), cancellationToken));
            })
            .WithName("ListMyExams")
            .Produces<IReadOnlyList<StudentExamDto>>();

        group.MapGet("/result/{examId:guid}", async (
                Guid examId,
                ICurrentUser currentUser,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is not { } userId)
                {
                    return Results.Unauthorized();
                }

                return Results.Ok(await dispatcher.QueryAsync(
                    new GetMyResultQuery(examId, userId), cancellationToken));
            })
            .WithName("GetMyExamResult")
            .Produces<AttemptReviewDto>();

        group.MapPost("/start/{examId:guid}", async (
                Guid examId,
                ICurrentUser currentUser,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is not { } userId)
                {
                    return Results.Unauthorized();
                }

                return Results.Ok(await dispatcher.SendAsync(
                    new StartAttemptCommand(examId, userId), cancellationToken));
            })
            .WithName("StartAttempt")
            .Produces<AttemptSheetDto>();

        group.MapPut("/{id:guid}/answers", async (
                Guid id,
                SaveAnswersRequest request,
                ICurrentUser currentUser,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is not { } userId)
                {
                    return Results.Unauthorized();
                }

                return Results.Ok(await dispatcher.SendAsync(
                    new SaveAnswersCommand(id, userId, request.Answers ?? []), cancellationToken));
            })
            .WithName("SaveAttemptAnswers")
            .Produces<AttemptSheetDto>();

        group.MapPost("/{id:guid}/submit", async (
                Guid id,
                ICurrentUser currentUser,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is not { } userId)
                {
                    return Results.Unauthorized();
                }

                return Results.Ok(await dispatcher.SendAsync(
                    new SubmitAttemptCommand(id, userId), cancellationToken));
            })
            .WithName("SubmitAttempt")
            .Produces<AttemptSheetDto>();

        group.MapPut("/{id:guid}/review", async (
                Guid id,
                ReviewAnswerRequest request,
                ICurrentUser currentUser,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is not { } userId)
                {
                    return Results.Unauthorized();
                }

                return Results.Ok(await dispatcher.SendAsync(
                    new ReviewAnswerCommand(id, request.QuestionId, request.Points, request.Feedback, userId),
                    cancellationToken));
            })
            .RequireAuthorization(AuthorizationPolicies.TeacherOrAdmin)
            .WithName("ReviewAttemptAnswer")
            .Produces<AttemptDto>();

        group.MapPut("/{id:guid}/release", async (
                Guid id,
                ReleaseAttemptRequest request,
                ICurrentUser currentUser,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is not { } userId)
                {
                    return Results.Unauthorized();
                }

                return Results.Ok(await dispatcher.SendAsync(
                    new ReleaseAttemptCommand(id, request.Mark, userId), cancellationToken));
            })
            .RequireAuthorization(AuthorizationPolicies.TeacherOrAdmin)
            .WithName("ReleaseAttempt")
            .Produces<AttemptDto>();
    }
}

internal sealed record CreateQuestionRequest(
    Guid SubjectId,
    QuestionKind Kind,
    string Text,
    string? Explanation,
    IReadOnlyList<OptionInput>? Options);

internal sealed record CreateExamRequest(
    Guid TeachingAssignmentId,
    string Title,
    string? Description,
    int DurationMinutes);

internal sealed record AddExamItemRequest(Guid QuestionId, int Points);

internal sealed record AddExamQuestionRequest(
    QuestionKind Kind,
    string Text,
    string? Explanation,
    IReadOnlyList<OptionInput>? Options,
    int Points);

internal sealed record PublishExamRequest(DateTimeOffset? OpensAt, DateTimeOffset? ClosesAt);

internal sealed record SaveAnswersRequest(IReadOnlyList<AnswerInput>? Answers);

internal sealed record ReviewAnswerRequest(Guid QuestionId, int Points, string? Feedback);

internal sealed record ReleaseAttemptRequest(decimal? Mark);
