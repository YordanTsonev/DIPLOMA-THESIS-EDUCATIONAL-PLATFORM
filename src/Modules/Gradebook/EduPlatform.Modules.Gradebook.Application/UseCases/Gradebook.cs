using EduPlatform.BuildingBlocks.Application.Messaging;
using EduPlatform.BuildingBlocks.Domain;
using EduPlatform.Modules.Gradebook.Application.Abstractions;
using EduPlatform.Modules.Gradebook.Contracts;
using EduPlatform.Modules.Gradebook.Domain;
using EduPlatform.Modules.SchoolStructure.Contracts;
using FluentValidation;

namespace EduPlatform.Modules.Gradebook.Application.UseCases;

public sealed record GetClassGradebookQuery(Guid TeachingAssignmentId, Guid TeacherUserId, string Role)
    : IQuery<ClassGradebookDto>;

public sealed record ClassGradebookDto(
    Guid TeachingAssignmentId,
    string SubjectName,
    string SchoolClassName,
    IReadOnlyList<StudentRowDto> Students);

public sealed record StudentRowDto(
    Guid StudentUserId,
    string StudentName,
    IReadOnlyList<GradeDto> Grades,
    decimal? Average,
    int UnexcusedAbsences,
    int ExcusedAbsences,
    int LateArrivals);

internal sealed class GetClassGradebookHandler(
    IGradebookReadStore store,
    ISchoolDirectory school) : IQueryHandler<GetClassGradebookQuery, ClassGradebookDto>
{
    public async Task<ClassGradebookDto> HandleAsync(
        GetClassGradebookQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var assignment = await school.FindAssignmentAsync(query.TeachingAssignmentId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new DomainException("Teaching assignment not found.");

        if (query.Role == "Teacher" && assignment.TeacherUserId != query.TeacherUserId)
        {
            throw new ForbiddenException("You do not teach this subject to this class.");
        }

        var students = await school
            .ListClassStudentsAsync(assignment.SchoolClassId, cancellationToken)
            .ConfigureAwait(false);

        var grades = await store
            .ListGradesAsync(query.TeachingAssignmentId, cancellationToken)
            .ConfigureAwait(false);

        var absences = await store
            .ListAbsencesAsync(query.TeachingAssignmentId, cancellationToken)
            .ConfigureAwait(false);

        var rows = students.Select(student =>
        {
            var own = grades.Where(grade => grade.StudentUserId == student.StudentUserId).ToList();
            var missed = absences.Where(absence => absence.StudentUserId == student.StudentUserId).ToList();

            return new StudentRowDto(
                student.StudentUserId,
                student.FullName,
                own,
                Average(own),
                missed.Count(item => item.Kind == nameof(AbsenceKind.Unexcused)),
                missed.Count(item => item.Kind == nameof(AbsenceKind.Excused)),
                missed.Count(item => item.Kind == nameof(AbsenceKind.Late)));
        }).ToList();

        return new ClassGradebookDto(
            assignment.Id, assignment.SubjectName, assignment.SchoolClassName, rows);
    }

    internal static decimal? Average(IReadOnlyCollection<GradeDto> grades) =>
        grades.Count == 0 ? null : Math.Round(grades.Average(grade => grade.Value), 2, MidpointRounding.AwayFromZero);
}

public sealed record GetStudentReportQuery(Guid StudentUserId) : IQuery<StudentReportDto>;

internal sealed class GetStudentReportHandler(
    IGradebookReadStore store,
    ISchoolDirectory school) : IQueryHandler<GetStudentReportQuery, StudentReportDto>
{
    public async Task<StudentReportDto> HandleAsync(
        GetStudentReportQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var assignments = await school
            .ListAssignmentsForUserAsync(query.StudentUserId, "Student", cancellationToken)
            .ConfigureAwait(false);

        var grades = await store
            .ListGradesForStudentAsync(query.StudentUserId, cancellationToken)
            .ConfigureAwait(false);

        var absences = await store
            .ListAbsencesForStudentAsync(query.StudentUserId, cancellationToken)
            .ConfigureAwait(false);

        var subjects = assignments.Select(assignment =>
        {
            var own = grades.Where(grade => grade.TeachingAssignmentId == assignment.Id).ToList();
            var missed = absences.Where(item => item.TeachingAssignmentId == assignment.Id).ToList();

            return new SubjectPerformanceDto(
                assignment.Id,
                assignment.SubjectName,
                own,
                GetClassGradebookHandler.Average(own),
                missed.Count(item => item.Kind == nameof(AbsenceKind.Unexcused)),
                missed.Count(item => item.Kind == nameof(AbsenceKind.Excused)),
                missed.Count(item => item.Kind == nameof(AbsenceKind.Late)));
        }).ToList();

        var withGrades = subjects.Where(subject => subject.Average is not null).ToList();

        var overall = withGrades.Count == 0
            ? (decimal?)null
            : Math.Round(withGrades.Average(subject => subject.Average!.Value), 2, MidpointRounding.AwayFromZero);

        var name = grades.Select(grade => grade.StudentName).FirstOrDefault() ?? string.Empty;

        return new StudentReportDto(query.StudentUserId, name, subjects, overall);
    }
}

public sealed record GiveGradeCommand(
    Guid StudentUserId,
    Guid TeachingAssignmentId,
    decimal Value,
    GradeKind Kind,
    GradeReason Reason,
    string? Comment,
    Guid TeacherUserId) : ICommand<GradeDto>;

internal sealed class GiveGradeValidator : AbstractValidator<GiveGradeCommand>
{
    public GiveGradeValidator()
    {
        RuleFor(command => command.StudentUserId).NotEmpty();
        RuleFor(command => command.TeachingAssignmentId).NotEmpty();
        RuleFor(command => command.Value).InclusiveBetween(Grade.MinValue, Grade.MaxValue);
        RuleFor(command => command.Kind).IsInEnum();
        RuleFor(command => command.Reason).IsInEnum();
        RuleFor(command => command.Comment).MaximumLength(Grade.MaxCommentLength);
    }
}

internal sealed class GiveGradeHandler(
    IGradebookRepository repository,
    IGradebookReadStore store,
    ISchoolDirectory school) : ICommandHandler<GiveGradeCommand, GradeDto>
{
    public async Task<GradeDto> HandleAsync(GiveGradeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        await EnsureTeachesAsync(school, command.TeacherUserId, command.TeachingAssignmentId, cancellationToken)
            .ConfigureAwait(false);

        await EnsureEnrolledAsync(school, command.TeachingAssignmentId, command.StudentUserId, cancellationToken)
            .ConfigureAwait(false);

        var grade = Grade.Give(
            command.StudentUserId,
            command.TeachingAssignmentId,
            command.Value,
            command.Kind,
            command.Reason,
            command.TeacherUserId,
            command.Comment);

        repository.AddGrade(grade);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return await store.FindGradeAsync(grade.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException("The grade could not be read back.");
    }

    internal static async Task EnsureTeachesAsync(
        ISchoolDirectory school,
        Guid teacherUserId,
        Guid teachingAssignmentId,
        CancellationToken cancellationToken)
    {
        var assignments = await school
            .ListAssignmentsForUserAsync(teacherUserId, "Teacher", cancellationToken)
            .ConfigureAwait(false);

        if (!assignments.Any(item => item.Id == teachingAssignmentId))
        {
            throw new DomainException("You do not teach this subject to this class.");
        }
    }

    internal static async Task EnsureEnrolledAsync(
        ISchoolDirectory school,
        Guid teachingAssignmentId,
        Guid studentUserId,
        CancellationToken cancellationToken)
    {
        var assignment = await school.FindAssignmentAsync(teachingAssignmentId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new DomainException("Teaching assignment not found.");

        var students = await school
            .ListClassStudentsAsync(assignment.SchoolClassId, cancellationToken)
            .ConfigureAwait(false);

        if (!students.Any(student => student.StudentUserId == studentUserId))
        {
            throw new DomainException("This student is not enrolled in the class.");
        }
    }
}

public sealed record DeleteGradeCommand(Guid GradeId, Guid TeacherUserId) : ICommand;

internal sealed class DeleteGradeHandler(
    IGradebookRepository repository,
    ISchoolDirectory school) : CommandHandler<DeleteGradeCommand>
{
    protected override async Task HandleCoreAsync(DeleteGradeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var grade = await repository.FindGradeAsync(command.GradeId, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException("Grade not found.");

        await GiveGradeHandler
            .EnsureTeachesAsync(school, command.TeacherUserId, grade.TeachingAssignmentId, cancellationToken)
            .ConfigureAwait(false);

        repository.RemoveGrade(grade);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

public sealed record RecordAbsenceCommand(
    Guid StudentUserId,
    Guid TeachingAssignmentId,
    DateOnly Date,
    int Period,
    AbsenceKind Kind,
    string? Note,
    Guid TeacherUserId) : ICommand<AbsenceDto>;

internal sealed class RecordAbsenceValidator : AbstractValidator<RecordAbsenceCommand>
{
    public RecordAbsenceValidator()
    {
        RuleFor(command => command.StudentUserId).NotEmpty();
        RuleFor(command => command.TeachingAssignmentId).NotEmpty();
        RuleFor(command => command.Period).InclusiveBetween(1, 10);
        RuleFor(command => command.Kind).IsInEnum();
        RuleFor(command => command.Note).MaximumLength(Absence.MaxNoteLength);
    }
}

internal sealed class RecordAbsenceHandler(
    IGradebookRepository repository,
    IGradebookReadStore store,
    ISchoolDirectory school) : ICommandHandler<RecordAbsenceCommand, AbsenceDto>
{
    public async Task<AbsenceDto> HandleAsync(RecordAbsenceCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        await GiveGradeHandler
            .EnsureTeachesAsync(school, command.TeacherUserId, command.TeachingAssignmentId, cancellationToken)
            .ConfigureAwait(false);

        await GiveGradeHandler
            .EnsureEnrolledAsync(school, command.TeachingAssignmentId, command.StudentUserId, cancellationToken)
            .ConfigureAwait(false);

        if (await repository.AbsenceExistsAsync(
                command.StudentUserId, command.TeachingAssignmentId, command.Date, command.Period, cancellationToken)
            .ConfigureAwait(false))
        {
            throw new DomainException("This absence is already recorded.");
        }

        var absence = Absence.Record(
            command.StudentUserId,
            command.TeachingAssignmentId,
            command.Date,
            command.Period,
            command.Kind,
            command.TeacherUserId,
            command.Note);

        repository.AddAbsence(absence);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return await store.FindAbsenceAsync(absence.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException("The absence could not be read back.");
    }
}

public sealed record ExcuseAbsenceCommand(Guid AbsenceId, string? Note, Guid TeacherUserId) : ICommand<AbsenceDto>;

internal sealed class ExcuseAbsenceHandler(
    IGradebookRepository repository,
    IGradebookReadStore store,
    ISchoolDirectory school) : ICommandHandler<ExcuseAbsenceCommand, AbsenceDto>
{
    public async Task<AbsenceDto> HandleAsync(ExcuseAbsenceCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var absence = await repository.FindAbsenceAsync(command.AbsenceId, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException("Absence not found.");

        await GiveGradeHandler
            .EnsureTeachesAsync(school, command.TeacherUserId, absence.TeachingAssignmentId, cancellationToken)
            .ConfigureAwait(false);

        absence.Excuse(command.Note);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return await store.FindAbsenceAsync(absence.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException("The absence could not be read back.");
    }
}
