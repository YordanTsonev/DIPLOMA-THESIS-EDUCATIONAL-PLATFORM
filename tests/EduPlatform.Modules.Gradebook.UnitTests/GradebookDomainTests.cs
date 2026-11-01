using EduPlatform.BuildingBlocks.Domain;
using EduPlatform.Modules.Gradebook.Domain;
using Shouldly;

namespace EduPlatform.Modules.Gradebook.UnitTests;

public sealed class GradeTests
{
    private static readonly Guid StudentId = Guid.CreateVersion7();
    private static readonly Guid AssignmentId = Guid.CreateVersion7();
    private static readonly Guid TeacherId = Guid.CreateVersion7();

    [Theory]
    [InlineData(1.99)]
    [InlineData(6.01)]
    [InlineData(0)]
    [InlineData(10)]
    public void Grades_outside_the_bulgarian_scale_are_rejected(decimal value)
    {
        Should.Throw<DomainException>(() => Give(value));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3.5)]
    [InlineData(5.75)]
    [InlineData(6)]
    public void Grades_inside_the_scale_are_accepted(decimal value)
    {
        Give(value).Value.ShouldBe(value);
    }

    [Fact]
    public void A_grade_is_rounded_to_two_decimals()
    {
        Give(4.567m).Value.ShouldBe(4.57m);
    }

    [Fact]
    public void Giving_a_grade_raises_the_event_notifications_listen_for()
    {
        Give(5m).DomainEvents.OfType<GradePublished>().ShouldHaveSingleItem();
    }

    [Fact]
    public void A_grade_can_be_corrected_within_the_scale()
    {
        var grade = Give(3m);

        grade.Correct(4m, "преоценена писмена работа");

        grade.Value.ShouldBe(4m);
        grade.Comment.ShouldBe("преоценена писмена работа");
    }

    [Fact]
    public void A_correction_outside_the_scale_is_rejected()
    {
        var grade = Give(3m);

        Should.Throw<DomainException>(() => grade.Correct(7m, null));

        grade.Value.ShouldBe(3m);
    }

    [Fact]
    public void A_grade_without_a_student_is_rejected()
    {
        Should.Throw<DomainException>(() => Grade.Give(
            Guid.Empty, AssignmentId, 5m, GradeKind.Current, GradeReason.Oral, TeacherId));
    }

    [Fact]
    public void A_blank_comment_is_stored_as_absent()
    {
        Grade.Give(StudentId, AssignmentId, 5m, GradeKind.Current, GradeReason.Oral, TeacherId, "   ")
            .Comment.ShouldBeNull();
    }

    private static Grade Give(decimal value) =>
        Grade.Give(StudentId, AssignmentId, value, GradeKind.Current, GradeReason.Oral, TeacherId);
}

public sealed class AbsenceTests
{
    private static readonly Guid StudentId = Guid.CreateVersion7();
    private static readonly Guid AssignmentId = Guid.CreateVersion7();
    private static readonly Guid TeacherId = Guid.CreateVersion7();

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public void An_absence_cannot_be_recorded_in_the_future()
    {
        Should.Throw<DomainException>(() => Record(Today.AddDays(1), AbsenceKind.Unexcused));
    }

    [Fact]
    public void A_late_arrival_counts_as_half_an_absence()
    {
        Record(Today, AbsenceKind.Late).Weight.ShouldBe(0.5m);
    }

    [Fact]
    public void A_full_absence_counts_as_one()
    {
        Record(Today, AbsenceKind.Unexcused).Weight.ShouldBe(1m);
    }

    [Fact]
    public void Excusing_an_absence_changes_its_kind_and_keeps_the_record()
    {
        var absence = Record(Today, AbsenceKind.Unexcused);
        var id = absence.Id;

        absence.Excuse("медицинска бележка");

        absence.Id.ShouldBe(id);
        absence.Kind.ShouldBe(AbsenceKind.Excused);
        absence.Note.ShouldBe("медицинска бележка");
    }

    [Fact]
    public void Excusing_twice_keeps_the_first_note()
    {
        var absence = Record(Today, AbsenceKind.Unexcused);
        absence.Excuse("първа бележка");

        absence.Excuse("втора бележка");

        absence.Note.ShouldBe("първа бележка");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void Periods_outside_the_school_day_are_rejected(int period)
    {
        Should.Throw<DomainException>(() => Absence.Record(
            StudentId, AssignmentId, Today, period, AbsenceKind.Unexcused, TeacherId));
    }

    [Fact]
    public void Recording_an_absence_raises_its_event()
    {
        Record(Today, AbsenceKind.Unexcused).DomainEvents.OfType<AbsenceRecorded>().ShouldHaveSingleItem();
    }

    private static Absence Record(DateOnly date, AbsenceKind kind) =>
        Absence.Record(StudentId, AssignmentId, date, 2, kind, TeacherId);
}
