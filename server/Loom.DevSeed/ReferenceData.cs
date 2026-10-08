using Loom.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Loom.DevSeed;

/// <summary>Home institution catalogue (programme, profiles, slots, courses) from database/reference-data.sql.</summary>
public static class ReferenceData
{
    public static async Task LoadAsync(AppDbContext db)
    {
        await using var stream = typeof(ReferenceData).Assembly.GetManifestResourceStream("reference-data.sql")
            ?? throw new InvalidOperationException("reference-data.sql is not embedded.");
        using var reader = new StreamReader(stream);
        var sql = (await reader.ReadToEndAsync()).TrimStart('﻿');

        // Raw ADO.NET: the script is not a format string, so it must not go through ExecuteSqlRaw.
        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }
}
