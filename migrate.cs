#:package Microsoft.Data.Sqlite@9.0.3

using Microsoft.Data.Sqlite;

var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "lol.db");
var dumpPath = Path.Combine(Directory.GetCurrentDirectory(), "old dump 06-17 - 06-30.txt");

using var conn = new SqliteConnection($"Data Source={dbPath}");
conn.Open();

// Wipe
using (var cmd = conn.CreateCommand())
{
    cmd.CommandText = "DELETE FROM lols";
    cmd.ExecuteNonQuery();
}

// Insert
int totalRows = 0;
using var tx = conn.BeginTransaction();
foreach (var line in File.ReadLines(dumpPath))
{
    var parts = line.Split('\t');
    if (parts.Length < 2) continue;
    var ts = parts[0].Trim();
    if (!int.TryParse(parts[1].Trim(), out int count)) continue;

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
