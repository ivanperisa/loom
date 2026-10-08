using Loom.Application.Features.Catalog;
using Loom.Domain.Entities;

namespace Loom.Application.Features.Completion;

public static class CompletionMappers
{
    public static RecognitionResponse ToResponse(this Recognition recognition) => new(
        recognition.Id,
        recognition.ExchangeId,
        recognition.Status.ToString(),
        recognition.Message,
        recognition.Entries.Select(e => e.ToResponse()).ToList(),
        recognition.CreatedAt,
        recognition.UpdatedAt,
        recognition.UpdatedAt,
        recognition.LastModifiedByUser?.Name,
        recognition.SignedAt,
        recognition.SignedByUser?.Name
    );

    public static RecognitionEntryResponse ToResponse(this RecognitionEntry entry)
    {
        var laEntry = entry.LearningAgreementEntry;
        var pc = laEntry.PartnerCourse!;
        var slot = laEntry.HomeSlot;
        var hours = pc.Hours();

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
            pc.Url,
            hours,
            pc.Ects,
            slotCourseIsvuCode,
            slotCourseName,
            slotCourseGroupIsvuCode,
            slotCourseGroupName,
            slot.SlotType.Color,
            slot.Semester,
            laEntry.AwardedEcts!.Value,
            entry.EnrollmentStatus,
            entry.OriginalGrade,
            entry.EctsGrade,
            entry.HrGrade,
            entry.ExamDate
        );
    }

    public static MappingSchemeEntryResponse ToResponse(this MappingSchemeEntry entry)
    {
        var pc = entry.PartnerCourse;
        var slot = entry.HomeSlot;
        var hours = pc?.Hours();

        return new(
            entry.Id,
            entry.HomeSlotId,
            entry.PartnerCourseId,
            pc?.Code ?? string.Empty,
            pc?.Name ?? string.Empty,
            pc?.NameHr,
            pc?.Url,
            hours,
            pc?.Ects ?? 0,
            slot.Course?.IsvuCode,
            slot.Course?.Name ?? string.Empty,
            slot.CourseGroup?.IsvuCode,
            slot.CourseGroup?.Name ?? string.Empty,
            slot.SlotType.Color,
            slot.Semester,
            entry.AwardedEcts ?? 0,
            entry.EnrollmentStatus?.ToString(),
            entry.OriginalGrade,
            entry.EctsGrade,
            entry.HrGrade,
            entry.ExamDate
        );
    }

    public static MappingSchemeResponse ToResponse(this IEnumerable<MappingSchemeEntry> entries, int exchangeId) =>
        new(exchangeId, entries.Select(e => e.ToResponse()).ToList());
}
