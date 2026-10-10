namespace Loom.Application.Features.Planning;

/// <summary>
/// What an LA version stores: ids (what restore uses) plus display copies (what history shows, even after courses
/// are renamed). Property names match the old snapshot JSON, so carried-over versions read the same way.
/// </summary>
public record LaVersionPayload(List<LaVersionEntry> Entries);

public record LaVersionEntry(
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
    string? PartnerCourseUrl = null);
