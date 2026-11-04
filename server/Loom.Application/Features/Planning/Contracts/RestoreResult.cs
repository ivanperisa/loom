namespace Loom.Application.Features.Planning;

/// <summary>Restore loads a version into the draft. Courses that no longer exist (or slots no longer in the profile) are reported, not guessed.</summary>
public record RestoreResult(int Entries, List<RestoreMissing> Missing);

public record RestoreMissing(int HomeSlotId, string HomeSlotLabel, string? PartnerCourseCode, string? PartnerCourseName, string Reason);
