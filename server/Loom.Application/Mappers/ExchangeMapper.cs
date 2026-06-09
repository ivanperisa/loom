using Loom.Application.DTOs.Exchange;
using Loom.Application.DTOs.LearningAgreement;
using Loom.Application.DTOs.Recognition;
using Loom.Domain.Entities;

namespace Loom.Application.Mappers;

public static class ExchangeMapper
{
    public static ExchangeResponse ToResponse(this Exchange exchange) => new(
        exchange.Id,
        exchange.Guid,
        exchange.StudentId,
        exchange.Student.Name,
        exchange.Student.Jmbag,
        exchange.HomeProfile.Program.Institution.Name,
        exchange.HomeProfile.Program.Name,
        exchange.HomeProfile.ToResponse(),
        exchange.PartnerInstitutionId,
        exchange.PartnerInstitution.Name,
        exchange.CoordinatorId,
        exchange.Coordinator?.Name,
        exchange.Student.Mentor,
        exchange.AcademicYear,
        exchange.SemesterType.ToString(),
        exchange.StudySemesters,
        exchange.CoordinatorMessage,
        exchange.EwpLink,
        string.IsNullOrEmpty(exchange.Student.Email),
        exchange.CreatedAt,
        exchange.UpdatedAt
    );

    public static ExchangeSummaryResponse ToSummaryResponse(this Exchange exchange) => new(
        exchange.Id,
        exchange.Guid,
        exchange.StudentId,
        exchange.Student.Name,
        exchange.Student.Jmbag,
        exchange.PartnerInstitution.Name,
        exchange.HomeProfile.Program.Institution.Name,
        exchange.HomeProfile.Program.Name,
        exchange.HomeProfile.Name,
        exchange.AcademicYear,
        exchange.SemesterType.ToString(),
        exchange.LearningAgreement!.Status.ToString(),
        exchange.Recognition?.Status.ToString(),
        exchange.EwpLink
    );

    public static HomeSlotResponse ToResponse(this HomeSlot slot) => new(
        slot.Id,
        slot.Semester,
        slot.SlotPosition,
        slot.Ects,
        slot.SlotTypeId,
        slot.SlotType.Name,
        slot.SlotType.NameEn,
        slot.SlotType.Color,
        slot.Course?.IsvuCode,
        slot.Course?.Name,
        slot.Course?.NameEn,
        slot.CourseGroup?.IsvuCode,
        slot.CourseGroup?.Name,
        slot.CourseGroup?.NameEn
    );

    public static LearningAgreementEntryResponse ToResponse(this LearningAgreementEntry entry) => new(
        entry.Id,
        entry.HomeSlotId,
        entry.Mode.ToString(),
        entry.PartnerCourseId,
        entry.PartnerCourse?.Code,
        entry.PartnerCourse?.Name,
        entry.PartnerCourse?.NameHr,
        entry.AwardedEcts,
        entry.IsDeleted
    );

    public static PartnerCourseResponse ToResponse(this PartnerCourse course) => new(
        course.Id,
        course.Code,
        course.Name,
        course.NameHr,
        course.Ects,
        course.LecturesH,
        course.AuditoryH,
        course.LabH,
        course.Semester.ToString(),
        course.Level.ToString()
    );

    public static RecognitionResponse ToResponse(this Recognition recognition) => new(
        recognition.Id,
        recognition.ExchangeId,
        recognition.Status.ToString(),
        recognition.Message,
        recognition.Entries.Select(e => e.ToResponse()).ToList(),
        recognition.CreatedAt,
        recognition.UpdatedAt
    );

    public static RecognitionEntryResponse ToResponse(this RecognitionEntry entry)
    {
        var laEntry = entry.LearningAgreementEntry;
        var pc = laEntry.PartnerCourse!;
        var slot = laEntry.HomeSlot;
        var hours = (pc.LecturesH.HasValue || pc.AuditoryH.HasValue || pc.LabH.HasValue)
            ? $"{pc.LecturesH ?? 0}/{pc.AuditoryH ?? 0}/{pc.LabH ?? 0}"
            : null;

        var slotCourseName = slot.Course?.Name ?? string.Empty;
        var slotCourseIsvuCode = slot.Course?.IsvuCode;
        var slotCourseGroupIsvuCode = slot.CourseGroup?.IsvuCode;
        var slotCourseGroupName = slot.CourseGroup?.Name ?? string.Empty;

        return new(
            entry.Id,
            entry.LearningAgreementEntryId,
            pc.Code,
            pc.Name,
            pc.NameHr,
            hours,
            pc.Ects,
            slotCourseIsvuCode,
            slotCourseName,
            slotCourseGroupIsvuCode,
            slotCourseGroupName,
            slot.SlotType.Color,
            slot.Semester,
            laEntry.AwardedEcts!.Value,
            entry.RecognizedAsCourseId,
            entry.RecognizedAsCourse?.Name,
            entry.EnrollmentStatus,
            entry.OriginalGrade,
            entry.EctsGrade,
            entry.HrGrade,
            entry.ExamDate,
            entry.IsRecognized
        );
    }
}
