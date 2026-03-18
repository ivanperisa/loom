using ExchangeMapper.Application.DTOs.CourseSlot;
using ExchangeMapper.Application.DTOs.Exchange;
using ExchangeMapper.Application.DTOs.Recognition;
using ExchangeMapper.Domain.Entities;

namespace ExchangeMapper.Application.Mappers;

public static class ExchangeMapper
{
    public static ExchangeResponse ToResponse(this Exchange exchange) => new(
        exchange.Id,
        exchange.StudentId,
        exchange.Student.Name,
        exchange.StudyProfile.ToResponse(),
        exchange.ForeignProgram.ToResponse(),
        exchange.CoordinatorId,
        exchange.Coordinator?.Name,
        exchange.Mentor,
        exchange.AcademicYear,
        exchange.SemesterType.ToString(),
        exchange.StudySemester,
        exchange.Status.ToString(),
        exchange.CreatedAt,
        exchange.UpdatedAt
    );

    public static ExchangeSummaryResponse ToSummaryResponse(this Exchange exchange) => new(
        exchange.Id,
        exchange.Student.Name,
        exchange.Student.Jmbag,
        exchange.ForeignProgram.Institution.Name,
        exchange.ForeignProgram.Name,
        exchange.StudyProfile.Name,
        exchange.AcademicYear,
        exchange.SemesterType.ToString(),
        exchange.Status.ToString()
    );

    public static CourseSlotResponse ToResponse(this CourseSlot slot) => new(
        slot.Id,
        slot.Semester,
        slot.ColStart,
        slot.Ects,
        slot.Category.ToString(),
        slot.CourseCode,
        slot.CourseName,
        slot.CourseNameEn,
        slot.Color
    );

    public static SlotStateResponse ToResponse(this SlotState state) => new(
        state.Id,
        state.CourseSlotId,
        state.Mode.ToString(),
        state.SlotMappings.Select(m => m.ToResponse()).ToList()
    );

    public static SlotMappingResponse ToResponse(this SlotMapping mapping) => new(
        mapping.Id,
        mapping.ForeignCourseId,
        mapping.ForeignCourse.Code,
        mapping.ForeignCourse.NameEn,
        mapping.ForeignCourse.NameHr,
        mapping.AwardedEcts
    );

    public static ForeignCourseResponse ToResponse(this ForeignCourse course) => new(
        course.Id,
        course.Code,
        course.NameEn,
        course.NameHr,
        course.Ects,
        course.LecturesH,
        course.AuditoryH,
        course.LabH
    );

    public static RecognitionResponse ToResponse(this Recognition recognition) => new(
        recognition.Id,
        recognition.ExchangeId,
        recognition.Status.ToString(),
        recognition.Entries.Select(e => e.ToResponse()).ToList(),
        recognition.CreatedAt,
        recognition.UpdatedAt
    );

    public static RecognitionEntryResponse ToResponse(this RecognitionEntry entry) => new(
        entry.Id,
        entry.SlotMappingId,
        entry.SlotMapping.ForeignCourse.Code,
        entry.SlotMapping.ForeignCourse.NameEn,
        entry.SlotMapping.AwardedEcts,
        entry.SlotMapping.SlotState.CourseSlot.CourseName,
        entry.EnrollmentStatus,
        entry.OriginalGrade,
        entry.EctsGrade,
        entry.HrGrade,
        entry.ExamDate
    );
}
