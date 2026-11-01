using EduPlatform.Modules.Gradebook.Contracts;
using EduPlatform.Modules.Gradebook.Domain;

namespace EduPlatform.Modules.Gradebook.Application.Abstractions;

public interface IGradebookRepository
{
    Task<Grade?> FindGradeAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Absence?> FindAbsenceAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> AbsenceExistsAsync(
        Guid studentUserId,
        Guid teachingAssignmentId,
        DateOnly lessonDate,
        int period,
        CancellationToken cancellationToken = default);

    void AddGrade(Grade grade);

    void RemoveGrade(Grade grade);

    void AddAbsence(Absence absence);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IGradebookReadStore
{
    Task<IReadOnlyList<GradeDto>> ListGradesAsync(
        Guid teachingAssignmentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GradeDto>> ListGradesForStudentAsync(
        Guid studentUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AbsenceDto>> ListAbsencesAsync(
        Guid teachingAssignmentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AbsenceDto>> ListAbsencesForStudentAsync(
        Guid studentUserId,
        CancellationToken cancellationToken = default);

    Task<GradeDto?> FindGradeAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AbsenceDto?> FindAbsenceAsync(Guid id, CancellationToken cancellationToken = default);
}
