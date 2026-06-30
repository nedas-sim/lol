#:package Microsoft.Data.Sqlite@9.0.3

using Microsoft.Data.Sqlite;
using System.Runtime.InteropServices;

const uint INPUT_KEYBOARD = 1;
const uint KEYEVENTF_UNICODE = 0x0004;
const uint KEYEVENTF_KEYUP = 0x0002;

var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "lol.db");
using var conn = new SqliteConnection($"Data Source={dbPath}");
conn.Open();

conn.ExecuteNonQuery("""
    CREATE TABLE IF NOT EXISTS lols (
        id        INTEGER PRIMARY KEY,
        lolled_at TEXT NOT NULL
    );
""");

using (var cmd = conn.CreateCommand())
{
    cmd.CommandText = "INSERT INTO lols (lolled_at) VALUES ($t)";
    cmd.Parameters.AddWithValue("$t", DateTime.UtcNow.ToString("o"));
    cmd.ExecuteNonQuery();
}

await Task.Delay(50);

const string phrase = ":lol:";
var inputs = new List<INPUT>();
foreach (char c in phrase)
{
    inputs.Add(new INPUT { type = INPUT_KEYBOARD, ki = new KEYBDINPUT { wScan = c, dwFlags = KEYEVENTF_UNICODE } });
    inputs.Add(new INPUT { type = INPUT_KEYBOARD, ki = new KEYBDINPUT { wScan = c, dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP } });
}
NativeMethods.SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf<INPUT>());

[StructLayout(LayoutKind.Sequential)]
struct INPUT { public uint type; public KEYBDINPUT ki; long padding; }

[StructLayout(LayoutKind.Sequential)]
struct KEYBDINPUT { public ushort wVk; public ushort wScan; public uint dwFlags; public uint time; public IntPtr dwExtraInfo; }

static class NativeMethods
{
    [DllImport("user32.dll")]
    public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
}

static class SqliteConnectionExtensions
{
    public static void ExecuteNonQuery(this SqliteConnection conn, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }
}
