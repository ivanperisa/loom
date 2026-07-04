using Loom.Application.DTOs.MappingScheme;
using Loom.Domain.Entities;

namespace Loom.Application.Mappers;

public static class MappingSchemeMapper
{
    public static MappingSchemeEntryResponse ToResponse(this MappingSchemeEntry entry)
    {
        var pc = entry.PartnerCourse;
        var slot = entry.HomeSlot;
        var hours = pc is not null && (pc.LecturesH.HasValue || pc.AuditoryH.HasValue || pc.LabH.HasValue)
            ? $"{pc.LecturesH ?? 0}/{pc.AuditoryH ?? 0}/{pc.LabH ?? 0}"
            : null;

        return new(
            entry.Id,
            entry.HomeSlotId,
            entry.PartnerCourseId,
            pc?.Code ?? string.Empty,
            pc?.Name ?? string.Empty,
            pc?.NameHr,
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
