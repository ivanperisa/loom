namespace Loom.Application.Features.Documents;

/// <summary>A row of a document as the diff sees it: a stable key (slot + course), labels for display, and comparable fields.</summary>
public sealed record DiffRow(
    int HomeSlotId,
    string HomeSlotLabel,
    int? PartnerCourseId,
    string? PartnerCourseCode,
    string? PartnerCourseName,
    IReadOnlyDictionary<string, string?> Fields);

/// <summary>Compares two versions of a document by stable ids (never by labels), field by field.</summary>
public static class DocumentDiff
{
    public const string Added = "Added", Removed = "Removed", Modified = "Modified";

    public static List<DocumentChange> Compare(IEnumerable<DiffRow> before, IEnumerable<DiffRow> after)
    {
        var old = before.GroupBy(Key).ToDictionary(g => g.Key, g => g.First());
        var current = after.GroupBy(Key).ToDictionary(g => g.Key, g => g.First());
        var changes = new List<DocumentChange>();

        foreach (var (key, row) in current)
        {
            if (!old.TryGetValue(key, out var previous))
            {
                changes.Add(Change(Added, row, row.Fields.Select(f => new FieldChange(f.Key, null, f.Value))));
                continue;
            }
            var fields = row.Fields.Keys.Union(previous.Fields.Keys)
                .Select(f => new FieldChange(f, previous.Fields.GetValueOrDefault(f), row.Fields.GetValueOrDefault(f)))
                .Where(f => f.Before != f.After)
                .ToList();
            if (fields.Count > 0) changes.Add(Change(Modified, row, fields));
        }
        foreach (var (key, row) in old)
            if (!current.ContainsKey(key))
                changes.Add(Change(Removed, row, row.Fields.Select(f => new FieldChange(f.Key, f.Value, null))));

        return changes
            .OrderBy(c => c.HomeSlotId).ThenBy(c => c.PartnerCourseCode).ThenBy(c => c.Type)
            .ToList();
    }

    private static (int, int?) Key(DiffRow row) => (row.HomeSlotId, row.PartnerCourseId);

    private static DocumentChange Change(string type, DiffRow row, IEnumerable<FieldChange> fields) =>
        new(type, row.HomeSlotId, row.HomeSlotLabel, row.PartnerCourseId, row.PartnerCourseCode, row.PartnerCourseName, fields.ToList());
}
