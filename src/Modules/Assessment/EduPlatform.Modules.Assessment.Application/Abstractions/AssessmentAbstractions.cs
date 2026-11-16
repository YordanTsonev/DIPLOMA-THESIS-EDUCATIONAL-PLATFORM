using EduPlatform.Modules.Assessment.Contracts;
using EduPlatform.Modules.Assessment.Domain;

namespace EduPlatform.Modules.Assessment.Application.Abstractions;

public interface IAssessmentRepository
{
    Task<Question?> FindQuestionAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Question>> ListQuestionsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default);

    Task<Exam?> FindExamAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Attempt?> FindAttemptAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Attempt?> FindAttemptOfStudentAsync(
        Guid examId,
        Guid studentUserId,
        CancellationToken cancellationToken = default);

    void AddQuestion(Question question);

    void RemoveQuestion(Question question);

    void AddExam(Exam exam);

    void RemoveExam(Exam exam);

    void AddAttempt(Attempt attempt);

    Task<bool> QuestionIsUsedAsync(Guid questionId, CancellationToken cancellationToken = default);

    Task<bool> ExamHasAttemptsAsync(Guid examId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Attempt>> ListExpiredAttemptsAsync(
        DateTimeOffset moment,
        int limit,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IAssessmentReadStore
{
    Task<IReadOnlyList<QuestionDto>> ListQuestionsAsync(
        IReadOnlyCollection<Guid> subjectIds,
        Guid? authorUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExamDto>> ListExamsAsync(
        IReadOnlyCollection<Guid> teachingAssignmentIds,
        CancellationToken cancellationToken = default);

    Task<ExamDto?> FindExamAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AttemptDto>> ListAttemptsAsync(
        Guid examId,
        CancellationToken cancellationToken = default);

    Task<AttemptDto?> FindAttemptAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StudentExamDto>> ListForStudentAsync(
        IReadOnlyCollection<Guid> teachingAssignmentIds,
        Guid studentUserId,
        CancellationToken cancellationToken = default);
}
