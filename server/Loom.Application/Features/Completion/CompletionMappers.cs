using System.Globalization;
using Loom.Application.Features.Catalog;
using Loom.Application.Features.Documents;
using Loom.Domain.Entities;

namespace Loom.Application.Features.Completion;

public static class CompletionMappers
{
    /// <summary>Needs RecognitionEntry.PartnerCourse and HomeSlot (type, course, group) loaded.</summary>
    public static MappingSchemeEntryResponse ToResponse(this MappingSchemeEntry entry)
    {
        var result = entry.RecognitionEntry;
        var pc = result.PartnerCourse;
        var slot = entry.HomeSlot;
        return new(
            entry.Id,
            entry.HomeSlotId,
            result.PartnerCourseId,
            pc.Code,
            pc.Name,
            pc.NameHr,
            pc.Url,
            pc.Hours(),
            pc.Ects,
            slot.Course?.IsvuCode,
            slot.Course?.Name ?? string.Empty,
            slot.CourseGroup?.IsvuCode,
            slot.CourseGroup?.Name ?? string.Empty,
            slot.SlotType.Color,
            slot.Semester,
            entry.AwardedEcts,
            result.EnrollmentStatus,
            result.OriginalGrade,
            result.EctsGrade,
            result.HrGrade,
            result.ExamDate
        );
    }

    public static MappingSchemeResponse ToResponse(this IEnumerable<MappingSchemeEntry> entries, int exchangeId) =>
        new(exchangeId, entries.Select(e => e.ToResponse()).ToList());

    public static RecognitionVersionEntry ToVersionEntry(this MappingSchemeEntry e)
    {
        var result = e.RecognitionEntry;
        return new(
            e.HomeSlotId, e.HomeSlot.Label, result.PartnerCourseId, result.PartnerCourse.Code, result.PartnerCourse.Name, e.AwardedEcts,
            result.EnrollmentStatus?.ToString(), result.OriginalGrade, result.EctsGrade, result.HrGrade, result.ExamDate);
    }

    public static string Hash(RecognitionVersionPayload payload) =>
        VersionStore.Hash(payload.Entries.Select(e => string.Create(CultureInfo.InvariantCulture,
            $"{e.HomeSlotId}|{e.PartnerCourseId}|{e.AwardedEcts:0.0}|{e.EnrollmentStatus}|{e.OriginalGrade}|{e.EctsGrade}|{e.HrGrade}|{e.ExamDate:yyyy-MM-dd}")));

    public static IEnumerable<DiffRow> DiffRows(RecognitionVersionPayload payload) =>
        payload.Entries.Select(e => new DiffRow(
            e.HomeSlotId, e.HomeSlotLabel, e.PartnerCourseId, e.PartnerCourseCode, e.PartnerCourseName,
            new Dictionary<string, string?>
            {
                ["ects"] = e.AwardedEcts?.ToString("0.0", CultureInfo.InvariantCulture),
                ["enrollmentStatus"] = e.EnrollmentStatus,
                ["originalGrade"] = e.OriginalGrade,
                ["ectsGrade"] = e.EctsGrade,
                ["hrGrade"] = e.HrGrade,
                ["examDate"] = e.ExamDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            }));
}
