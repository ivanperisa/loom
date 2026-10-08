namespace Loom.Application.Features.Planning;

/// <summary>
/// Derives amendment numbers from the approval snapshots: an entry's number is the approval it first appeared in
/// (0 = original agreement). Entries that were approved once but are gone now come back as removed rows.
/// (Phase 5 stores this per entry instead of replaying every snapshot.)
/// </summary>
public static class AmendmentHistory
{
    public static List<LearningAgreementEntryResponse> Apply(List<LearningAgreementEntryResponse> active, IReadOnlyList<LaSnapshotData> approvals)
    {
        if (approvals.Count == 0) return active;

        var lastSeenIn = new Dictionary<(int, int), (int Approval, LaSnapshotEntry Entry)>();
        var firstSeenIn = new Dictionary<(int, int), int>();
        for (var i = 0; i < approvals.Count; i++)
        {
            foreach (var entry in approvals[i].Entries.Where(e => e.PartnerCourseId.HasValue))
            {
                var key = (entry.HomeSlotId, entry.PartnerCourseId!.Value);
                lastSeenIn[key] = (i + 1, entry);
                firstSeenIn.TryAdd(key, i + 1);
            }
        }

        var numbered = active
            .Select(e => e.PartnerCourseId is int courseId && firstSeenIn.TryGetValue((e.HomeSlotId, courseId), out var first)
                ? e with { AmendmentNumber = first - 1 }
                : e)
            .ToList();

        var activeKeys = active.Where(e => e.PartnerCourseId.HasValue).Select(e => (e.HomeSlotId, e.PartnerCourseId!.Value)).ToHashSet();
        var removed = lastSeenIn
            .Where(kv => !activeKeys.Contains(kv.Key))
            .Select(kv => new LearningAgreementEntryResponse(
                0, kv.Value.Entry.HomeSlotId, kv.Value.Entry.Mode,
                kv.Value.Entry.PartnerCourseId, kv.Value.Entry.PartnerCourseCode, kv.Value.Entry.PartnerCourseName,
                kv.Value.Entry.PartnerCourseNameHr, kv.Value.Entry.PartnerCourseUrl,
                kv.Value.Entry.AwardedEcts, IsDeleted: true, AmendmentNumber: kv.Value.Approval));

        return [.. numbered, .. removed];
    }
}
