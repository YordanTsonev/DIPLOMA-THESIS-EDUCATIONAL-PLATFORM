using EduPlatform.BuildingBlocks.Application.Messaging;
using EduPlatform.Modules.Gradebook.Application.UseCases;
using EduPlatform.Modules.Gradebook.Contracts;
using EduPlatform.Modules.Gradebook.Domain;

namespace EduPlatform.Modules.Gradebook.Application;

public sealed class GradebookWriter(IDispatcher dispatcher) : IGradebookWriter
{
    public Task<GradeDto> GiveHomeworkGradeAsync(
        Guid studentUserId,
        Guid teachingAssignmentId,
        decimal value,
        Guid teacherUserId,
        string? comment,
        CancellationToken cancellationToken = default) =>
        dispatcher.SendAsync(
            new GiveGradeCommand(
                studentUserId,
                teachingAssignmentId,
                value,
                GradeKind.Current,
                GradeReason.Homework,
                comment,
                teacherUserId),
            cancellationToken);
}
