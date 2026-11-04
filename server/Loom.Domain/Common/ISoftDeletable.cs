namespace Loom.Domain.Common;

/// <summary>Rows that are hidden instead of deleted once something references them.</summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
}

public static class SoftDeletableExtensions
{
    public static void MarkDeleted(this ISoftDeletable entity)
    {
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
    }

    public static void Restore(this ISoftDeletable entity)
    {
        entity.IsDeleted = false;
        entity.DeletedAt = null;
    }
}
