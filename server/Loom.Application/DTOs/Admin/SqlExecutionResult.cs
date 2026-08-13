namespace Loom.Application.DTOs.Admin;

public record SqlExecutionResult(int RowsAffected, List<Dictionary<string, object?>>? Rows);
