using SQLite;
using PantryPal.Core.Migrations;

namespace PantryPal.Core.Data;

/// <summary>
/// Single entry point for SQLite. Call InitAsync() once at app start (safe to call multiple times).
/// </summary>
public sealed class PantryDatabase
{
    public string DbPath { get; }
    private SQLiteAsyncConnection? _conn;

    private readonly List<IMigration> _migrations = new()
    {
        new _0001_Initial(),
        new _0002_AddIndexes()
    };

    public PantryDatabase(string dbPath)
    {
        if (string.IsNullOrWhiteSpace(dbPath)) throw new ArgumentNullException(nameof(dbPath));
        DbPath = dbPath;

        var dir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }

    public async Task InitAsync()
    {
        if (_conn is null)
        {
            var flags = SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache;
            _conn = new SQLiteAsyncConnection(DbPath, flags);
            await ApplyPragmasAsync(_conn);
        }
        await RunMigrationsAsync();
    }

    public SQLiteAsyncConnection Connection => _conn ?? throw new InvalidOperationException("Call InitAsync() first.");

    public async Task RunInTransactionAsync(Func<SQLiteAsyncConnection, Task> work)
    {
        var c = Connection;
        await c.ExecuteAsync("BEGIN IMMEDIATE;");
        try
        {
            await work(c);
            await c.ExecuteAsync("COMMIT;");
        }
        catch
        {
            try { await c.ExecuteAsync("ROLLBACK;"); } catch { /* ignore */ }
            throw;
        }
    }

    private static async Task ApplyPragmasAsync(SQLiteAsyncConnection c)
    {
        try
        {
            await c.ExecuteAsync("PRAGMA foreign_keys = ON;");

            // IMPORTANT: journal_mode returns a value; consume with ExecuteScalarAsync to avoid "not an error".
            var mode = await c.ExecuteScalarAsync<string>("PRAGMA journal_mode = WAL;");
            if (!string.Equals(mode, "wal", StringComparison.OrdinalIgnoreCase))
            {
                try { await c.ExecuteScalarAsync<string>("PRAGMA journal_mode = DELETE;"); } catch { }
            }

            await c.ExecuteAsync("PRAGMA busy_timeout = 5000;");
        }
        catch
        {
            try { await c.ExecuteScalarAsync<string>("PRAGMA journal_mode = DELETE;"); } catch { }
        }
    }

    // --- schema versioning via PRAGMA user_version ---
    public Task<int> GetSchemaVersionAsync() => Connection.ExecuteScalarAsync<int>("PRAGMA user_version;");
    private Task SetSchemaVersionAsync(int v) => Connection.ExecuteAsync($"PRAGMA user_version = {v};");

    private async Task RunMigrationsAsync()
    {
        var current = await GetSchemaVersionAsync();
        foreach (var m in _migrations.OrderBy(x => x.FromVersion))
        {
            if (m.FromVersion == current)
            {
                await m.UpAsync(Connection);
                current = m.ToVersion;
                await SetSchemaVersionAsync(current);
            }
        }
    }
}
