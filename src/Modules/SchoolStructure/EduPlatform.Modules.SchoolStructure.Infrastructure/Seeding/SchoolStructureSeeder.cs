using EduPlatform.BuildingBlocks.Infrastructure.Seeding;
using EduPlatform.Modules.Identity.Contracts;
using EduPlatform.Modules.SchoolStructure.Domain;
using EduPlatform.Modules.SchoolStructure.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EduPlatform.Modules.SchoolStructure.Infrastructure.Seeding;

internal sealed class SchoolStructureSeeder(SchoolStructureDbContext dbContext, IUserDirectory users) : IDataSeeder
{
    private static readonly (string Name, string Code)[] Subjects =
    [
        ("Български език и литература", "BEL"),
        ("Математика", "MAT"),
        ("История и цивилизации", "IST"),
        ("География и икономика", "GEO"),
        ("Биология и здравно образование", "BIO"),
        ("Информационни технологии", "IT"),
        ("Английски език", "ENG"),
        ("Физическо възпитание и спорт", "FVS"),
    ];

    public int Order => 20;

    public string Name => "school structure";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var academicYear = await SeedAcademicYearAsync(cancellationToken).ConfigureAwait(false);
        await SeedSubjectsAsync(cancellationToken).ConfigureAwait(false);
        await SeedClassesAsync(academicYear, cancellationToken).ConfigureAwait(false);
        await SeedScheduleAsync(cancellationToken).ConfigureAwait(false);
        await SeedCalendarAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task SeedCalendarAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.CalendarEvents.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var author = await users.FindByEmailAsync("admin@eduplatform.local", cancellationToken).ConfigureAwait(false);

        if (author is null)
        {
            return;
        }

        var ninthA = await dbContext.SchoolClasses
            .FirstOrDefaultAsync(schoolClass => schoolClass.Grade == 9, cancellationToken)
            .ConfigureAwait(false);

        var mathematics = await dbContext.Subjects
            .SingleOrDefaultAsync(subject => subject.Code == "MAT", cancellationToken)
            .ConfigureAwait(false);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        dbContext.CalendarEvents.Add(CalendarEvent.Create(
            CalendarEventType.Event,
            "Родителска среща",
            today.AddDays(7),
            null,
            author.Id,
            description: "Начало 18:00 в актовата зала."));

        dbContext.CalendarEvents.Add(CalendarEvent.Create(
            CalendarEventType.Holiday,
            "Коледна ваканция",
            new DateOnly(today.Year, 12, 23),
            new DateOnly(today.Year + 1, 1, 3),
            author.Id));

        if (ninthA is not null && mathematics is not null)
        {
            dbContext.CalendarEvents.Add(CalendarEvent.Create(
                CalendarEventType.Test,
                "Контролно по математика",
                today.AddDays(3),
                null,
                author.Id,
                ninthA.Id,
                mathematics.Id,
                period: 2,
                description: "Квадратни уравнения."));
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task SeedScheduleAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.ScheduleSlots.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var assignments = await dbContext.TeachingAssignments
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (assignments.Count == 0)
        {
            return;
        }

        var weekdays = new[]
        {
            DayOfWeek.Monday,
            DayOfWeek.Tuesday,
            DayOfWeek.Wednesday,
            DayOfWeek.Thursday,
            DayOfWeek.Friday,
        };

        foreach (var assignment in assignments)
        {
            var placed = 0;

            foreach (var day in weekdays)
            {
                if (placed >= assignment.WeeklyHours)
                {
                    break;
                }

                var period = placed + 1;
                var (startsAt, endsAt) = SchoolPeriods.For(period);

                dbContext.ScheduleSlots.Add(ScheduleSlot.Create(
                    assignment.Id, day, period, startsAt, endsAt, "каб. 201"));

                placed++;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<AcademicYear> SeedAcademicYearAsync(CancellationToken cancellationToken)
    {
        var existing = await dbContext.AcademicYears
            .SingleOrDefaultAsync(year => year.Name == "2026/2027", cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            return existing;
        }

        var academicYear = AcademicYear.Create(
            "2026/2027", new DateOnly(2026, 9, 15), new DateOnly(2027, 6, 30));

        academicYear.MarkCurrent();
        dbContext.AcademicYears.Add(academicYear);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return academicYear;
    }

    private async Task SeedSubjectsAsync(CancellationToken cancellationToken)
    {
        var existingCodes = await dbContext.Subjects
            .Select(subject => subject.Code)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var known = existingCodes.ToHashSet(StringComparer.Ordinal);
        var added = false;

        foreach (var (name, code) in Subjects.Where(subject => !known.Contains(subject.Code)))
        {
            dbContext.Subjects.Add(Subject.Create(name, code));
            added = true;
        }

        if (added)
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task SeedClassesAsync(AcademicYear academicYear, CancellationToken cancellationToken)
    {
        if (await dbContext.SchoolClasses.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var teacher = await FindUserByEmailAsync("teacher@eduplatform.local", cancellationToken).ConfigureAwait(false);
        var student = await FindUserByEmailAsync("student@eduplatform.local", cancellationToken).ConfigureAwait(false);

        var ninthA = SchoolClass.Create(academicYear.Id, 9, "А", teacher?.Id);
        var tenthB = SchoolClass.Create(academicYear.Id, 10, "Б");

        if (student is not null)
        {
            ninthA.Enroll(student.Id);
        }

        dbContext.SchoolClasses.AddRange(ninthA, tenthB);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (teacher is null)
        {
            return;
        }

        var mathematics = await dbContext.Subjects
            .SingleOrDefaultAsync(subject => subject.Code == "MAT", cancellationToken)
            .ConfigureAwait(false);

        if (mathematics is not null)
        {
            dbContext.TeachingAssignments.Add(
                TeachingAssignment.Create(teacher.Id, mathematics.Id, ninthA.Id, 4));

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private Task<UserDto?> FindUserByEmailAsync(string email, CancellationToken cancellationToken) =>
        users.FindByEmailAsync(email, cancellationToken);
}
