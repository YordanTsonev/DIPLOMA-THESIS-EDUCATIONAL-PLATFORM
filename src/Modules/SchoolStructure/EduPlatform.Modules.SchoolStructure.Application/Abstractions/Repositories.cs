using EduPlatform.Modules.SchoolStructure.Contracts;
using EduPlatform.Modules.SchoolStructure.Domain;

namespace EduPlatform.Modules.SchoolStructure.Application.Abstractions;

public interface ISchoolStructureRepository
{
    Task<AcademicYear?> FindAcademicYearAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AcademicYear?> FindCurrentAcademicYearAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AcademicYear>> ListAcademicYearsAsync(CancellationToken cancellationToken = default);

    Task<Subject?> FindSubjectAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> SubjectCodeExistsAsync(string code, CancellationToken cancellationToken = default);

    Task<SchoolClass?> FindClassAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ClassExistsAsync(Guid academicYearId, int grade, string section, CancellationToken cancellationToken = default);

    Task<bool> AssignmentExistsAsync(Guid subjectId, Guid schoolClassId, CancellationToken cancellationToken = default);

    Task<TeachingAssignment?> FindAssignmentAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ScheduleSlot>> ListSlotsForClassAsync(Guid schoolClassId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ScheduleSlot>> ListSlotsForTeacherAsync(Guid teacherUserId, CancellationToken cancellationToken = default);

    void AddSlot(ScheduleSlot slot);

    void RemoveSlot(ScheduleSlot slot);

    Task<ScheduleSlot?> FindSlotAsync(Guid id, CancellationToken cancellationToken = default);

    void AddAcademicYear(AcademicYear academicYear);

    void AddSubject(Subject subject);

    void AddClass(SchoolClass schoolClass);

    void AddAssignment(TeachingAssignment assignment);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface ISchoolStructureReadStore
{
    Task<IReadOnlyList<AcademicYearDto>> ListAcademicYearsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SubjectDto>> ListSubjectsAsync(bool includeInactive, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SchoolClassDto>> ListClassesAsync(Guid? academicYearId, CancellationToken cancellationToken = default);

    Task<SchoolClassDto?> FindClassAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClassStudentDto>> ListClassStudentsAsync(Guid schoolClassId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TeachingAssignmentDto>> ListAssignmentsAsync(Guid? schoolClassId, Guid? teacherUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ScheduleSlotDto>> ListScheduleAsync(Guid? schoolClassId, Guid? teacherUserId, CancellationToken cancellationToken = default);

    Task<Guid?> FindClassOfStudentAsync(Guid studentUserId, CancellationToken cancellationToken = default);
}
