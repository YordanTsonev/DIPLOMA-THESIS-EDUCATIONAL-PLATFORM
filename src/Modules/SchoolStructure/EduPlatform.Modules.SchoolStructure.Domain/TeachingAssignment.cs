using EduPlatform.BuildingBlocks.Domain;

namespace EduPlatform.Modules.SchoolStructure.Domain;

public sealed class TeachingAssignment : AggregateRoot
{
    public const int MaxWeeklyHours = 40;

    private TeachingAssignment(Guid id, Guid teacherUserId, Guid subjectId, Guid schoolClassId, int weeklyHours)
        : base(id)
    {
        TeacherUserId = teacherUserId;
        SubjectId = subjectId;
        SchoolClassId = schoolClassId;
        WeeklyHours = weeklyHours;
    }

    private TeachingAssignment() { }

    public Guid TeacherUserId { get; private set; }

    public Guid SubjectId { get; private set; }

    public Guid SchoolClassId { get; private set; }

    public int WeeklyHours { get; private set; }

    public static TeachingAssignment Create(Guid teacherUserId, Guid subjectId, Guid schoolClassId, int weeklyHours)
    {
        if (teacherUserId == Guid.Empty)
        {
            throw new DomainException("A teacher is required.");
        }

        if (subjectId == Guid.Empty)
        {
            throw new DomainException("A subject is required.");
        }

        if (schoolClassId == Guid.Empty)
        {
            throw new DomainException("A class is required.");
        }

        if (weeklyHours is < 1 or > MaxWeeklyHours)
        {
            throw new DomainException($"Weekly hours must be between 1 and {MaxWeeklyHours}.");
        }

        var assignment = new TeachingAssignment(
            Guid.CreateVersion7(), teacherUserId, subjectId, schoolClassId, weeklyHours);

        assignment.Raise(new TeachingAssignmentCreated(assignment.Id, teacherUserId, subjectId, schoolClassId));
        return assignment;
    }

    public void ChangeWeeklyHours(int weeklyHours)
    {
        if (weeklyHours is < 1 or > MaxWeeklyHours)
        {
            throw new DomainException($"Weekly hours must be between 1 and {MaxWeeklyHours}.");
        }

        WeeklyHours = weeklyHours;
    }
}
