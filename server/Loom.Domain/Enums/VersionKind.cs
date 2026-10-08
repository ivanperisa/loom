namespace Loom.Domain.Enums;

public enum VersionKind
{
    /// <summary>Created when the coordinator approves; numbered and never changed afterwards.</summary>
    Approved,
    /// <summary>Restore point taken before an import or restore replaces the draft.</summary>
    Backup
}
