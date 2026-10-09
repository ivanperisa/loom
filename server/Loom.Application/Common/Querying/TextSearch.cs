namespace Loom.Application.Common.Querying;

/// <summary>SQL functions for search, mapped in <c>AppDbContext</c>. They only work inside a LINQ-to-SQL query.</summary>
public static class TextSearch
{
    /// <summary>
    /// <c>f_unaccent(text)</c>: PostgreSQL's <c>unaccent</c> in an immutable wrapper, so expression indexes can use it
    /// ("Čakovec" → "Cakovec", "Đuro" → "Duro").
    /// </summary>
    public static string Unaccent(string? value) => throw new InvalidOperationException("Only for use in database queries.");
}
