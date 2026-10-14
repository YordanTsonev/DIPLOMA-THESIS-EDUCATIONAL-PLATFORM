using EduPlatform.BuildingBlocks.Domain;

namespace EduPlatform.Modules.SchoolStructure.Domain;

public sealed class SchoolClass : AggregateRoot
{
    public const int MinGrade = 1;
    public const int MaxGrade = 12;
    public const int MaxSectionLength = 4;

    private readonly List<ClassEnrollment> _enrollments = [];

    private SchoolClass(Guid id, Guid academicYearId, int grade, string section, Guid? homeroomTeacherId)
        : base(id)
    {
        AcademicYearId = academicYearId;
        Grade = grade;
        Section = section;
        HomeroomTeacherId = homeroomTeacherId;
    }

    private SchoolClass() { }

    public Guid AcademicYearId { get; private set; }

    public int Grade { get; private set; }

    public string Section { get; private set; } = null!;

    public Guid? HomeroomTeacherId { get; private set; }

    public string DisplayName => $"{Grade}{Section}";

    public IReadOnlyCollection<ClassEnrollment> Enrollments => _enrollments.AsReadOnly();

    public int ActiveStudentCount => _enrollments.Count(enrollment => enrollment.IsActive);

    public static SchoolClass Create(Guid academicYearId, int grade, string? section, Guid? homeroomTeacherId = null)
    {
        if (academicYearId == Guid.Empty)
        {
            throw new DomainException("A class must belong to an academic year.");
        }

        if (grade is < MinGrade or > MaxGrade)
        {
            throw new DomainException($"Grade must be between {MinGrade} and {MaxGrade}.");
        }

        var trimmed = section?.Trim().ToUpperInvariant() ?? string.Empty;

        if (trimmed.Length == 0)
        {
            throw new DomainException("Class section is required.");
        }

        if (trimmed.Length > MaxSectionLength)
        {
            throw new DomainException($"Class section must not exceed {MaxSectionLength} characters.");
        }

        var schoolClass = new SchoolClass(Guid.CreateVersion7(), academicYearId, grade, trimmed, homeroomTeacherId);
        schoolClass.Raise(new SchoolClassCreated(schoolClass.Id, academicYearId, schoolClass.DisplayName));
        return schoolClass;
    }

    public void AssignHomeroomTeacher(Guid? teacherUserId)
    {
        HomeroomTeacherId = teacherUserId == Guid.Empty ? null : teacherUserId;
    }

    public ClassEnrollment Enroll(Guid studentUserId)
    {
        if (studentUserId == Guid.Empty)
        {
            throw new DomainException("A student is required.");
        }

        var existing = _enrollments.SingleOrDefault(enrollment => enrollment.StudentUserId == studentUserId);

        if (existing is not null)
        {
            if (existing.IsActive)
            {
                throw new DomainException("This student is already enrolled in the class.");
            }

            existing.Restore();
            Raise(new StudentEnrolled(Id, studentUserId));
            return existing;
        }

        var enrollment = ClassEnrollment.Create(Id, studentUserId);
        _enrollments.Add(enrollment);
        Raise(new StudentEnrolled(Id, studentUserId));
        return enrollment;
    }

    public void Withdraw(Guid studentUserId)
    {
        var enrollment = _enrollments.SingleOrDefault(candidate => candidate.StudentUserId == studentUserId)
            ?? throw new DomainException("This student is not enrolled in the class.");

        if (!enrollment.IsActive)
        {
            return;
        }

        enrollment.Withdraw();
        Raise(new StudentWithdrawn(Id, studentUserId));
    }
}
