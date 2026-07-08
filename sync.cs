#:package Microsoft.Data.Sqlite@9.0.3

using Microsoft.Data.Sqlite;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: dotnet run sync.cs -- <from-date> <dump-file>");
    return 1;
}

var fromDate = args[0]; // e.g. "2026-07-05"
var dumpPath = args[1];

var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "lol.db");
using var conn = new SqliteConnection($"Data Source={dbPath}");
conn.Open();

using (var cmd = conn.CreateCommand())
{
    cmd.CommandText = "DELETE FROM lols WHERE lolled_at >= $from";
    cmd.Parameters.AddWithValue("$from", fromDate);
    var deleted = cmd.ExecuteNonQuery();
    Console.WriteLine($"Deleted {deleted} rows from {fromDate} onward.");
}

int totalRows = 0;
using var tx = conn.BeginTransaction();
foreach (var line in File.ReadLines(dumpPath))
{
    var parts = line.Split('\t');
    if (parts.Length < 3) continue;
    var ts = parts[1].Trim();
    if (!int.TryParse(parts[2].Trim(), out int count)) continue;

    for (int i = 0; i < count; i++)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "INSERT INTO lols (lolled_at) VALUES ($t)";
        cmd.Parameters.AddWithValue("$t", ts);
        cmd.ExecuteNonQuery();
        totalRows++;
    }
}
tx.Commit();

Console.WriteLine($"Done. Inserted {totalRows} rows.");
return 0;
