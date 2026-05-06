using Loom.Application.DTOs.CourseSlot;
using Loom.Application.DTOs.Exchange;
using Loom.Application.DTOs.Recognition;
using Loom.Domain.Entities;

namespace Loom.Application.Mappers;

public static class ExchangeMapper
{
    public static ExchangeResponse ToResponse(this Exchange exchange) => new(
        exchange.Id,
        exchange.StudentId,
        exchange.Student.Name,
        exchange.Student.Jmbag,
        exchange.StudyProfile.StudyProgram.Institution.Name,
        exchange.StudyProfile.StudyProgram.Name,
        exchange.StudyProfile.ToResponse(),
        exchange.ForeignProgram.ToResponse(),
        exchange.CoordinatorId,
        exchange.Coordinator?.Name,
        exchange.Student.Mentor,
        exchange.AcademicYear,
        exchange.SemesterType.ToString(),
        exchange.StudySemester,
        exchange.Status.ToString(),
        exchange.CoordinatorMessage,
        exchange.CreatedAt,
        exchange.UpdatedAt
    );

    public static ExchangeSummaryResponse ToSummaryResponse(this Exchange exchange) => new(
        exchange.Id,
        exchange.StudentId,
        exchange.Student.Name,
        exchange.Student.Jmbag,
        exchange.ForeignProgram.Institution.Name,
        exchange.ForeignProgram.Name,
        exchange.StudyProfile.StudyProgram.Institution.Name,
        exchange.StudyProfile.StudyProgram.Name,
        exchange.StudyProfile.Name,
        exchange.AcademicYear,
        exchange.SemesterType.ToString(),
        exchange.Status.ToString(),
        exchange.Recognition?.Status.ToString()
    );

    public static CourseSlotResponse ToResponse(this CourseSlot slot) => new(
        slot.Id,
        slot.Semester,
        slot.SlotPosition,
        slot.Ects,
        slot.CategoryCode,
        slot.Category.Name,
        slot.Category.NameEn,
        slot.Category.Color,
        slot.CourseCode,
        slot.CourseName,
        slot.CourseNameEn
    );

    public static LearningAgreementEntryResponse ToResponse(this LearningAgreementEntry entry) => new(
        entry.Id,
        entry.CourseSlotId,
        entry.Mode.ToString(),
        entry.ForeignCourseId,
        entry.ForeignCourse?.Code,
        entry.ForeignCourse?.NameEn,
        entry.ForeignCourse?.NameHr,
        entry.AwardedEcts
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

    public static ExchangeSnapshotResponse ToResponse(this ExchangeSnapshot snapshot) => new(
        snapshot.Id,
        snapshot.ExchangeId,
        snapshot.Phase.ToString(),
        snapshot.ChangedById,
        snapshot.ChangedBy.Name,
        snapshot.CreatedAt,
        null
    );

    public static RecognitionResponse ToResponse(this Recognition recognition) => new(
        recognition.Id,
        recognition.ExchangeId,
        recognition.Status.ToString(),
        recognition.Entries.Select(e => e.ToResponse()).ToList(),
        recognition.CreatedAt,
        recognition.UpdatedAt
    );

    public static RecognitionEntryResponse ToResponse(this RecognitionEntry entry)
    {
        var laEntry = entry.LearningAgreementEntry;
        var fc = laEntry.ForeignCourse!;
        var slot = laEntry.CourseSlot;
        var hours = (fc.LecturesH.HasValue || fc.AuditoryH.HasValue || fc.LabH.HasValue)
            ? $"{fc.LecturesH ?? 0}/{fc.AuditoryH ?? 0}/{fc.LabH ?? 0}"
            : null;
        return new(
            entry.Id,
            entry.LearningAgreementEntryId,
            fc.Code,
            fc.NameEn,
            fc.NameHr,
            fc.Ects,
            hours,
            laEntry.AwardedEcts!.Value,
            slot.CourseName,
            slot.CourseCode,
            slot.CategoryCode,
            slot.Category.Name,
            slot.Category.Color,
            slot.Semester,
            entry.EnrollmentStatus,
            entry.OriginalGrade,
            entry.EctsGrade,
            entry.HrGrade,
            entry.ExamDate,
            entry.IsRecognized
        );
    }
}
