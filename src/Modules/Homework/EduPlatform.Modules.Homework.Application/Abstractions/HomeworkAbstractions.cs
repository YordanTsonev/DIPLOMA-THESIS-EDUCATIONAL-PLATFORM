using EduPlatform.Modules.Homework.Contracts;
using EduPlatform.Modules.Homework.Domain;

namespace EduPlatform.Modules.Homework.Application.Abstractions;

public interface IHomeworkRepository
{
    Task<Assignment?> FindAssignmentAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Submission?> FindSubmissionAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Submission?> FindSubmissionOfStudentAsync(
        Guid assignmentId,
        Guid studentUserId,
        CancellationToken cancellationToken = default);

    void AddAssignment(Assignment assignment);

    void RemoveAssignment(Assignment assignment);

    void AddSubmission(Submission submission);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IHomeworkReadStore
{
    Task<IReadOnlyList<AssignmentDto>> ListAssignmentsAsync(
        IReadOnlyCollection<Guid> teachingAssignmentIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SubmissionDto>> ListSubmissionsAsync(
        Guid assignmentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StudentAssignmentDto>> ListForStudentAsync(
        IReadOnlyCollection<Guid> teachingAssignmentIds,
        Guid studentUserId,
        CancellationToken cancellationToken = default);

    Task<SubmissionDto?> FindSubmissionAsync(Guid id, CancellationToken cancellationToken = default);
}
