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
