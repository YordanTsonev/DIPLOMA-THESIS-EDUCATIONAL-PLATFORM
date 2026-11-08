using EduPlatform.BuildingBlocks.Application.Messaging;
using EduPlatform.Modules.Gradebook.Application.Abstractions;
using EduPlatform.Modules.Gradebook.Application.UseCases;
using EduPlatform.Modules.Gradebook.Contracts;
using EduPlatform.Modules.Gradebook.Domain;

namespace EduPlatform.Modules.Gradebook.Application;

public sealed class GradebookWriter(IDispatcher dispatcher, IGradebookRepository repository)
    : IGradebookWriter
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

    public async Task RemoveHomeworkGradeAsync(Guid gradeId, CancellationToken cancellationToken = default)
    {
        var grade = await repository.FindGradeAsync(gradeId, cancellationToken).ConfigureAwait(false);

        if (grade is null)
        {
            return;
        }

        repository.RemoveGrade(grade);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
