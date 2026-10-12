using EduPlatform.BuildingBlocks.Application.Messaging;
using EduPlatform.BuildingBlocks.Domain;
using EduPlatform.Modules.Identity.Contracts;
using EduPlatform.Modules.SchoolStructure.Application.Abstractions;
using EduPlatform.Modules.SchoolStructure.Contracts;
using EduPlatform.Modules.SchoolStructure.Domain;
using FluentValidation;

namespace EduPlatform.Modules.SchoolStructure.Application.UseCases;

public sealed record ListClassesQuery(Guid? AcademicYearId = null) : IQuery<IReadOnlyList<SchoolClassDto>>;

internal sealed class ListClassesHandler(ISchoolStructureReadStore store)
    : IQueryHandler<ListClassesQuery, IReadOnlyList<SchoolClassDto>>
{
    public Task<IReadOnlyList<SchoolClassDto>> HandleAsync(ListClassesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return store.ListClassesAsync(query.AcademicYearId, cancellationToken);
    }
}

public sealed record GetClassStudentsQuery(Guid SchoolClassId) : IQuery<IReadOnlyList<ClassStudentDto>>;

internal sealed class GetClassStudentsHandler(ISchoolStructureReadStore store)
    : IQueryHandler<GetClassStudentsQuery, IReadOnlyList<ClassStudentDto>>
{
    public Task<IReadOnlyList<ClassStudentDto>> HandleAsync(
        GetClassStudentsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return store.ListClassStudentsAsync(query.SchoolClassId, cancellationToken);
    }
}

public sealed record CreateClassCommand(Guid AcademicYearId, int Grade, string Section, Guid? HomeroomTeacherId)
    : ICommand<SchoolClassDto>;

internal sealed class CreateClassValidator : AbstractValidator<CreateClassCommand>
{
    public CreateClassValidator()
    {
        RuleFor(command => command.AcademicYearId).NotEmpty();
        RuleFor(command => command.Grade).InclusiveBetween(SchoolClass.MinGrade, SchoolClass.MaxGrade);
        RuleFor(command => command.Section).NotEmpty().MaximumLength(SchoolClass.MaxSectionLength);
    }
}

internal sealed class CreateClassHandler(
    ISchoolStructureRepository repository,
    ISchoolStructureReadStore store,
    IUserDirectory users) : ICommandHandler<CreateClassCommand, SchoolClassDto>
{
    public async Task<SchoolClassDto> HandleAsync(CreateClassCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        _ = await repository.FindAcademicYearAsync(command.AcademicYearId, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException("Academic year not found.");

        var section = command.Section.Trim().ToUpperInvariant();

        if (await repository.ClassExistsAsync(command.AcademicYearId, command.Grade, section, cancellationToken).ConfigureAwait(false))
        {
            throw new DomainException($"Class {command.Grade}{section} already exists for this academic year.");
        }

        if (command.HomeroomTeacherId is { } teacherId)
        {
            await EnsureTeacherAsync(users, teacherId, cancellationToken).ConfigureAwait(false);
        }

        var schoolClass = SchoolClass.Create(
            command.AcademicYearId, command.Grade, command.Section, command.HomeroomTeacherId);

        repository.AddClass(schoolClass);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return await store.FindClassAsync(schoolClass.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException("The class could not be read back after creation.");
    }

    internal static async Task EnsureTeacherAsync(
        IUserDirectory users,
        Guid teacherUserId,
        CancellationToken cancellationToken)
    {
        var teacher = await users.FindByIdAsync(teacherUserId, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException("Teacher not found.");

        if (!string.Equals(teacher.Role, "Teacher", StringComparison.Ordinal))
        {
            throw new DomainException("Only a user with the Teacher role can be assigned to a class.");
        }

        if (!teacher.IsActive)
        {
            throw new DomainException("This teacher account is deactivated.");
        }
    }
}

public sealed record EnrollStudentCommand(Guid SchoolClassId, Guid StudentUserId) : ICommand;

internal sealed class EnrollStudentValidator : AbstractValidator<EnrollStudentCommand>
{
    public EnrollStudentValidator()
    {
        RuleFor(command => command.SchoolClassId).NotEmpty();
        RuleFor(command => command.StudentUserId).NotEmpty();
    }
}

internal sealed class EnrollStudentHandler(
    ISchoolStructureRepository repository,
    IUserDirectory users) : CommandHandler<EnrollStudentCommand>
{
    protected override async Task HandleCoreAsync(EnrollStudentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var schoolClass = await repository.FindClassAsync(command.SchoolClassId, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException("Class not found.");

        var student = await users.FindByIdAsync(command.StudentUserId, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException("Student not found.");

        if (!string.Equals(student.Role, "Student", StringComparison.Ordinal))
        {
            throw new DomainException("Only a user with the Student role can be enrolled.");
        }

        if (!student.IsActive)
        {
            throw new DomainException("This student account is deactivated.");
        }

        schoolClass.Enroll(command.StudentUserId);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

public sealed record WithdrawStudentCommand(Guid SchoolClassId, Guid StudentUserId) : ICommand;

internal sealed class WithdrawStudentHandler(ISchoolStructureRepository repository)
    : CommandHandler<WithdrawStudentCommand>
{
    protected override async Task HandleCoreAsync(WithdrawStudentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var schoolClass = await repository.FindClassAsync(command.SchoolClassId, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException("Class not found.");

        schoolClass.Withdraw(command.StudentUserId);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
