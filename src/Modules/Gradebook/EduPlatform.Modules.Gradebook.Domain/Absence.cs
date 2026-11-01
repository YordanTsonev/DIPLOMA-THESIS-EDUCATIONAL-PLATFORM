using EduPlatform.BuildingBlocks.Domain;

namespace EduPlatform.Modules.Gradebook.Domain;

public enum AbsenceKind
{
    Unexcused = 1,
    Excused = 2,
    Late = 3,
}

public sealed class Absence : AggregateRoot
{
    public const int MaxNoteLength = 300;

    private Absence(
        Guid id,
        Guid studentUserId,
        Guid teachingAssignmentId,
        DateOnly date,
        int period,
        AbsenceKind kind,
        string? note,
        Guid teacherUserId)
        : base(id)
    {
        StudentUserId = studentUserId;
        TeachingAssignmentId = teachingAssignmentId;
        Date = date;
        Period = period;
        Kind = kind;
        Note = note;
        TeacherUserId = teacherUserId;
        RecordedAt = DateTimeOffset.UtcNow;
    }

    private Absence() { }

    public Guid StudentUserId { get; private set; }

    public Guid TeachingAssignmentId { get; private set; }

    public DateOnly Date { get; private set; }

    public int Period { get; private set; }

    public AbsenceKind Kind { get; private set; }

    public string? Note { get; private set; }

    public Guid TeacherUserId { get; private set; }

    public DateTimeOffset RecordedAt { get; private set; }

    public decimal Weight => Kind == AbsenceKind.Late ? 0.5m : 1m;

    public static Absence Record(
        Guid studentUserId,
        Guid teachingAssignmentId,
        DateOnly date,
        int period,
        AbsenceKind kind,
        Guid teacherUserId,
        string? note = null)
    {
        if (studentUserId == Guid.Empty)
        {
            throw new DomainException("A student is required.");
        }

        if (teachingAssignmentId == Guid.Empty)
        {
            throw new DomainException("A teaching assignment is required.");
        }

        if (teacherUserId == Guid.Empty)
        {
            throw new DomainException("A teacher is required.");
        }

        if (!Enum.IsDefined(kind))
        {
            throw new DomainException("Unknown absence kind.");
        }

        if (period is < 1 or > 10)
        {
            throw new DomainException("Period must be between 1 and 10.");
        }

        if (date > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new DomainException("An absence cannot be recorded for a future date.");
        }

        var absence = new Absence(
            Guid.CreateVersion7(),
            studentUserId,
            teachingAssignmentId,
            date,
            period,
            kind,
            NormaliseNote(note),
            teacherUserId);

        absence.Raise(new AbsenceRecorded(absence.Id, studentUserId, teachingAssignmentId, date));
        return absence;
    }

    public void Excuse(string? note)
    {
        if (Kind == AbsenceKind.Excused)
        {
            return;
        }

        Kind = AbsenceKind.Excused;
        Note = NormaliseNote(note) ?? Note;
    }

    private static string? NormaliseNote(string? note)
    {
        var trimmed = note?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        return trimmed.Length > MaxNoteLength
            ? throw new DomainException($"Note must not exceed {MaxNoteLength} characters.")
            : trimmed;
    }
}
