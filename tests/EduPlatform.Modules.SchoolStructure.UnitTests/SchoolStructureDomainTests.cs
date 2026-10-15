using EduPlatform.BuildingBlocks.Domain;
using EduPlatform.Modules.SchoolStructure.Domain;
using Shouldly;

namespace EduPlatform.Modules.SchoolStructure.UnitTests;

public sealed class SchoolClassTests
{
    private static readonly Guid AcademicYearId = Guid.CreateVersion7();

    [Fact]
    public void A_class_is_displayed_as_grade_and_section()
    {
        CreateClass(9, "а").DisplayName.ShouldBe("9А");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void Grades_outside_the_school_range_are_rejected(int grade)
    {
        Should.Throw<DomainException>(() => SchoolClass.Create(AcademicYearId, grade, "А"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_section_is_required(string? section)
    {
        Should.Throw<DomainException>(() => SchoolClass.Create(AcademicYearId, 9, section));
    }

    [Fact]
    public void Enrolling_a_student_counts_them_in_the_class()
    {
        var schoolClass = CreateClass();

        schoolClass.Enroll(Guid.CreateVersion7());

        schoolClass.ActiveStudentCount.ShouldBe(1);
        schoolClass.DomainEvents.OfType<StudentEnrolled>().Count().ShouldBe(1);
    }

    [Fact]
    public void The_same_student_cannot_be_enrolled_twice()
    {
        var schoolClass = CreateClass();
        var student = Guid.CreateVersion7();
        schoolClass.Enroll(student);

        Should.Throw<DomainException>(() => schoolClass.Enroll(student));
    }

    [Fact]
    public void A_withdrawn_student_stops_counting_but_keeps_their_record()
    {
        var schoolClass = CreateClass();
        var student = Guid.CreateVersion7();
        schoolClass.Enroll(student);

        schoolClass.Withdraw(student);

        schoolClass.ActiveStudentCount.ShouldBe(0);
        schoolClass.Enrollments.Count.ShouldBe(1);
    }

    [Fact]
    public void A_withdrawn_student_can_be_enrolled_again()
    {
        var schoolClass = CreateClass();
        var student = Guid.CreateVersion7();
        schoolClass.Enroll(student);
        schoolClass.Withdraw(student);

        schoolClass.Enroll(student);

        schoolClass.ActiveStudentCount.ShouldBe(1);
        schoolClass.Enrollments.Count.ShouldBe(1);
    }

    [Fact]
    public void Withdrawing_someone_who_was_never_enrolled_is_rejected()
    {
        Should.Throw<DomainException>(() => CreateClass().Withdraw(Guid.CreateVersion7()));
    }

    private static SchoolClass CreateClass(int grade = 9, string section = "А") =>
        SchoolClass.Create(AcademicYearId, grade, section);
}

public sealed class SubjectTests
{
    [Fact]
    public void Codes_are_stored_in_upper_case()
    {
        Subject.Create("Математика", "mat").Code.ShouldBe("MAT");
    }

    [Theory]
    [InlineData(null, "MAT")]
    [InlineData("  ", "MAT")]
    [InlineData("Математика", null)]
    [InlineData("Математика", "   ")]
    public void A_name_and_a_code_are_both_required(string? name, string? code)
    {
        Should.Throw<DomainException>(() => Subject.Create(name, code));
    }

    [Fact]
    public void A_deactivated_subject_can_be_brought_back()
    {
        var subject = Subject.Create("Математика", "MAT");
        subject.Deactivate();

        subject.Reactivate();

        subject.IsActive.ShouldBeTrue();
    }
}

public sealed class AcademicYearTests
{
    [Fact]
    public void A_year_must_end_after_it_starts()
    {
        Should.Throw<DomainException>(() => AcademicYear.Create(
            "2026/2027", new DateOnly(2027, 6, 30), new DateOnly(2026, 9, 15)));
    }

    [Fact]
    public void A_year_covers_the_dates_between_its_bounds()
    {
        var year = AcademicYear.Create("2026/2027", new DateOnly(2026, 9, 15), new DateOnly(2027, 6, 30));

        year.Covers(new DateOnly(2027, 1, 20)).ShouldBeTrue();
        year.Covers(new DateOnly(2027, 8, 1)).ShouldBeFalse();
    }
}

public sealed class TeachingAssignmentTests
{
    [Fact]
    public void An_assignment_links_a_teacher_a_subject_and_a_class()
    {
        var assignment = TeachingAssignment.Create(
            Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), 4);

        assignment.WeeklyHours.ShouldBe(4);
        assignment.DomainEvents.OfType<TeachingAssignmentCreated>().ShouldHaveSingleItem();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(41)]
    public void Weekly_hours_outside_a_sane_range_are_rejected(int hours)
    {
        Should.Throw<DomainException>(() => TeachingAssignment.Create(
            Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), hours));
    }

    [Fact]
    public void An_assignment_without_a_teacher_is_rejected()
    {
        Should.Throw<DomainException>(() => TeachingAssignment.Create(
            Guid.Empty, Guid.CreateVersion7(), Guid.CreateVersion7(), 4));
    }
}

public sealed class ScheduleSlotTests
{
    private static readonly Guid AssignmentId = Guid.CreateVersion7();

    [Fact]
    public void A_slot_takes_the_bell_times_it_is_given()
    {
        var slot = CreateSlot(DayOfWeek.Monday, 1);

        slot.StartsAt.ShouldBe(new TimeOnly(8, 0));
        slot.EndsAt.ShouldBe(new TimeOnly(8, 40));
    }

    [Theory]
    [InlineData(DayOfWeek.Saturday)]
    [InlineData(DayOfWeek.Sunday)]
    public void Lessons_cannot_fall_at_the_weekend(DayOfWeek day)
    {
        Should.Throw<DomainException>(() => CreateSlot(day, 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void Periods_outside_the_school_day_are_rejected(int period)
    {
        Should.Throw<DomainException>(() => ScheduleSlot.Create(
            AssignmentId, DayOfWeek.Monday, period, new TimeOnly(8, 0), new TimeOnly(8, 40)));
    }

    [Fact]
    public void A_lesson_must_end_after_it_starts()
    {
        Should.Throw<DomainException>(() => ScheduleSlot.Create(
            AssignmentId, DayOfWeek.Monday, 1, new TimeOnly(9, 0), new TimeOnly(8, 0)));
    }

    [Fact]
    public void Two_slots_at_the_same_time_on_the_same_day_overlap()
    {
        CreateSlot(DayOfWeek.Monday, 1).Overlaps(CreateSlot(DayOfWeek.Monday, 1)).ShouldBeTrue();
    }

    [Fact]
    public void The_same_period_on_different_days_does_not_overlap()
    {
        CreateSlot(DayOfWeek.Monday, 1).Overlaps(CreateSlot(DayOfWeek.Tuesday, 1)).ShouldBeFalse();
    }

    [Fact]
    public void Consecutive_periods_do_not_overlap()
    {
        CreateSlot(DayOfWeek.Monday, 1).Overlaps(CreateSlot(DayOfWeek.Monday, 2)).ShouldBeFalse();
    }

    [Fact]
    public void A_moved_slot_keeps_its_identity()
    {
        var slot = CreateSlot(DayOfWeek.Monday, 1);
        var id = slot.Id;

        var (start, end) = SchoolPeriods.For(3);
        slot.Move(DayOfWeek.Thursday, 3, start, end);

        slot.Id.ShouldBe(id);
        slot.DayOfWeek.ShouldBe(DayOfWeek.Thursday);
        slot.Period.ShouldBe(3);
    }

    [Fact]
    public void A_blank_room_is_stored_as_absent()
    {
        var (start, end) = SchoolPeriods.For(1);

        ScheduleSlot.Create(AssignmentId, DayOfWeek.Monday, 1, start, end, "   ").Room.ShouldBeNull();
    }

    private static ScheduleSlot CreateSlot(DayOfWeek day, int period)
    {
        var (start, end) = SchoolPeriods.For(period);
        return ScheduleSlot.Create(AssignmentId, day, period, start, end);
    }
}

public sealed class CalendarEventTests
{
    private static readonly Guid ClassId = Guid.CreateVersion7();
    private static readonly Guid AuthorId = Guid.CreateVersion7();
    private static readonly DateOnly Day = new(2026, 11, 10);

    [Fact]
    public void A_single_day_event_ends_on_the_day_it_starts()
    {
        var calendarEvent = CalendarEvent.Create(
            CalendarEventType.Event, "Родителска среща", Day, null, AuthorId);

        calendarEvent.EndsOn.ShouldBe(Day);
        calendarEvent.IsSchoolWide.ShouldBeTrue();
    }

    [Fact]
    public void A_test_must_belong_to_a_class()
    {
        Should.Throw<DomainException>(() => CalendarEvent.Create(
            CalendarEventType.Test, "Контролно", Day, null, AuthorId));
    }

    [Fact]
    public void A_test_tied_to_a_class_is_accepted()
    {
        var calendarEvent = CalendarEvent.Create(
            CalendarEventType.Test, "Контролно", Day, null, AuthorId, ClassId, period: 3);

        calendarEvent.Period.ShouldBe(3);
        calendarEvent.IsSchoolWide.ShouldBeFalse();
    }

    [Fact]
    public void An_event_cannot_end_before_it_starts()
    {
        Should.Throw<DomainException>(() => CalendarEvent.Create(
            CalendarEventType.Holiday, "Ваканция", Day, Day.AddDays(-2), AuthorId));
    }

    [Fact]
    public void A_multi_day_event_cannot_be_tied_to_one_period()
    {
        Should.Throw<DomainException>(() => CalendarEvent.Create(
            CalendarEventType.Event, "Седмица на книгата", Day, Day.AddDays(4), AuthorId, ClassId, period: 2));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void A_title_is_required(string? title)
    {
        Should.Throw<DomainException>(() => CalendarEvent.Create(
            CalendarEventType.Event, title, Day, null, AuthorId));
    }

    [Fact]
    public void A_holiday_covers_every_day_in_its_span()
    {
        var holiday = CalendarEvent.Create(
            CalendarEventType.Holiday, "Коледна ваканция", Day, Day.AddDays(6), AuthorId);

        holiday.OccursOn(Day.AddDays(3)).ShouldBeTrue();
        holiday.OccursOn(Day.AddDays(7)).ShouldBeFalse();
    }

    [Fact]
    public void An_event_is_found_by_a_range_that_touches_it()
    {
        var holiday = CalendarEvent.Create(
            CalendarEventType.Holiday, "Ваканция", Day, Day.AddDays(6), AuthorId);

        holiday.OverlapsRange(Day.AddDays(5), Day.AddDays(20)).ShouldBeTrue();
        holiday.OverlapsRange(Day.AddDays(7), Day.AddDays(20)).ShouldBeFalse();
    }

    [Fact]
    public void Rescheduling_keeps_the_event_identity()
    {
        var calendarEvent = CalendarEvent.Create(
            CalendarEventType.Event, "Екскурзия", Day, null, AuthorId);
        var id = calendarEvent.Id;

        calendarEvent.Reschedule(Day.AddDays(14), Day.AddDays(15));

        calendarEvent.Id.ShouldBe(id);
        calendarEvent.EndsOn.ShouldBe(Day.AddDays(15));
    }
}
