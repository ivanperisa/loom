using System.Text.Json;
using ExchangeMapper.Application.DTOs.Exchange;
using ExchangeMapper.Domain.Entities;

namespace ExchangeMapper.Application.Mappers;

public static class ExchangeMapper
{
    public static ExchangeResponse ToResponse(this Exchange e) => new()
    {
        Id = e.Id,
        AcademicYear = e.AcademicYear,
        Semester = e.Semester.ToString(),
        DurationMonths = e.DurationMonths,
        Mentor = e.Mentor,
        Status = e.Status.ToString(),
        ForeignInstitution = e.ForeignInstitution.ToResponse(),
        Courses = e.ExchangeCourses.Select(c => c.ToResponse()).ToList()
    };

    public static ExchangeCourseResponse ToResponse(this ExchangeCourse c) => new()
    {
        Id = c.Id,
        Code = c.Code,
        Name = c.Name,
        NameEn = c.NameEn,
        Ects = c.Ects,
        Status = c.Status.ToString(),
        LecturesHours = c.LecturesHours,
        AuditoryHours = c.AuditoryHours,
        LabHours = c.LabHours,
        OriginalGrade = c.OriginalGrade,
        EctsGrade = c.EctsGrade,
        ExamDate = c.ExamDate,
        Mappings = c.CourseMappings.Select(m => m.ToResponse()).ToList()
    };

    public static MappingHistoryResponse ToResponse(this MappingHistory h)
    {
        MappingSnapshotResponse? snapshot = null;
        try { snapshot = JsonSerializer.Deserialize<MappingSnapshotResponse>(h.Snapshot); }
        catch { snapshot = new MappingSnapshotResponse { Status = "Unknown" }; }

        return new MappingHistoryResponse
        {
            Id = h.Id,
            ChangedByName = h.ChangedByUser.Name,
            CreatedAt = h.CreatedAt,
            ExchangeCourseName = h.CourseMapping.ExchangeCourse.NameEn ?? h.CourseMapping.ExchangeCourse.Name,
            ExchangeCourseCode = h.CourseMapping.ExchangeCourse.Code,
            Snapshot = snapshot!
        };
    }

    public static CourseMappingResponse ToResponse(this CourseMapping m) => new()
    {
        Id = m.Id,
        CourseId = m.CourseId,
        CourseName = m.Course.Name,
        CourseCode = m.Course.Code,
        AwardedEcts = m.AwardedEcts,
        ConvertedGrade = m.ConvertedGrade,
        Status = m.Status.ToString(),
        CoordinatorNote = m.CoordinatorNote
    };
}
