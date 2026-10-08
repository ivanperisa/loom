namespace Loom.Application.Features.Planning;

public record LaSnapshotEntry(
    int HomeSlotId,
    string HomeSlotLabel,
    int HomeSlotSemester,
    int HomeSlotEcts,
    string Mode,
    int? PartnerCourseId,
    string? PartnerCourseCode,
    string? PartnerCourseName,
    decimal? AwardedEcts,
    string? PartnerCourseNameHr = null,
    string? PartnerCourseUrl = null
);
