using EduPlatform.Modules.SchoolStructure.Application.Abstractions;
using EduPlatform.Modules.SchoolStructure.Domain;
using Microsoft.EntityFrameworkCore;

namespace EduPlatform.Modules.SchoolStructure.Infrastructure.Persistence;

internal sealed class SchoolStructureRepository(SchoolStructureDbContext dbContext) : ISchoolStructureRepository
{
    public Task<AcademicYear?> FindAcademicYearAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.AcademicYears.SingleOrDefaultAsync(year => year.Id == id, cancellationToken);

    public Task<AcademicYear?> FindCurrentAcademicYearAsync(CancellationToken cancellationToken = default) =>
        dbContext.AcademicYears.SingleOrDefaultAsync(year => year.IsCurrent, cancellationToken);

    public async Task<IReadOnlyList<AcademicYear>> ListAcademicYearsAsync(CancellationToken cancellationToken = default) =>
        await dbContext.AcademicYears
            .OrderByDescending(year => year.StartsOn)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public Task<Subject?> FindSubjectAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Subjects.SingleOrDefaultAsync(subject => subject.Id == id, cancellationToken);

    public Task<bool> SubjectCodeExistsAsync(string code, CancellationToken cancellationToken = default) =>
        dbContext.Subjects.AnyAsync(subject => subject.Code == code, cancellationToken);

    public Task<SchoolClass?> FindClassAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.SchoolClasses
            .Include(schoolClass => schoolClass.Enrollments)
            .SingleOrDefaultAsync(schoolClass => schoolClass.Id == id, cancellationToken);

    public Task<bool> ClassExistsAsync(
        Guid academicYearId,
        int grade,
        string section,
        CancellationToken cancellationToken = default) =>
        dbContext.SchoolClasses.AnyAsync(
            schoolClass => schoolClass.AcademicYearId == academicYearId
                && schoolClass.Grade == grade
                && schoolClass.Section == section,
            cancellationToken);

    public Task<bool> AssignmentExistsAsync(
        Guid subjectId,
        Guid schoolClassId,
        CancellationToken cancellationToken = default) =>
        dbContext.TeachingAssignments.AnyAsync(
            assignment => assignment.SubjectId == subjectId && assignment.SchoolClassId == schoolClassId,
            cancellationToken);

    public Task<TeachingAssignment?> FindAssignmentAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.TeachingAssignments.SingleOrDefaultAsync(assignment => assignment.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ScheduleSlot>> ListSlotsForClassAsync(
        Guid schoolClassId,
        CancellationToken cancellationToken = default) =>
        await dbContext.ScheduleSlots
            .Where(slot => dbContext.TeachingAssignments
                .Any(assignment => assignment.Id == slot.TeachingAssignmentId
                    && assignment.SchoolClassId == schoolClassId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<ScheduleSlot>> ListSlotsForTeacherAsync(
        Guid teacherUserId,
        CancellationToken cancellationToken = default) =>
        await dbContext.ScheduleSlots
            .Where(slot => dbContext.TeachingAssignments
                .Any(assignment => assignment.Id == slot.TeachingAssignmentId
                    && assignment.TeacherUserId == teacherUserId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public Task<ScheduleSlot?> FindSlotAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.ScheduleSlots.SingleOrDefaultAsync(slot => slot.Id == id, cancellationToken);

    public void AddSlot(ScheduleSlot slot) => dbContext.ScheduleSlots.Add(slot);

    public void AddCalendarEvent(CalendarEvent calendarEvent) => dbContext.CalendarEvents.Add(calendarEvent);

    public void RemoveCalendarEvent(CalendarEvent calendarEvent) => dbContext.CalendarEvents.Remove(calendarEvent);

    public Task<CalendarEvent?> FindCalendarEventAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.CalendarEvents.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

    public void RemoveSlot(ScheduleSlot slot) => dbContext.ScheduleSlots.Remove(slot);

    public void AddAcademicYear(AcademicYear academicYear) => dbContext.AcademicYears.Add(academicYear);

    public void AddSubject(Subject subject) => dbContext.Subjects.Add(subject);

    public void AddClass(SchoolClass schoolClass) => dbContext.SchoolClasses.Add(schoolClass);

    public void AddAssignment(TeachingAssignment assignment) => dbContext.TeachingAssignments.Add(assignment);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
