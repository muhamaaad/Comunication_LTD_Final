using Microsoft.Data.Sqlite;

// One-off: the communicationltd.db was originally created via EF's EnsureCreated(),
// so it has no __EFMigrationsHistory row for the tables that already exist.
// This inserts that row so `dotnet ef database update` only applies migrations
// that are genuinely new, instead of trying to re-CREATE TABLEs that already exist.

var dbPath = args.Length > 0 ? args[0] : Path.Combine("..", "..", "communicationltd.db");
if (!File.Exists(dbPath))
{
    Console.Error.WriteLine($"DB not found at '{dbPath}' (resolved: '{Path.GetFullPath(dbPath)}').");
    return 1;
}

const string migrationId = "20260811142924_AddUserEntity";
const string productVersion = "8.0.8";

using var conn = new SqliteConnection($"Data Source={dbPath}");
conn.Open();

using (var createHistory = conn.CreateCommand())
{
    createHistory.CommandText = @"CREATE TABLE IF NOT EXISTS ""__EFMigrationsHistory"" (
        ""MigrationId"" TEXT NOT NULL CONSTRAINT ""PK___EFMigrationsHistory"" PRIMARY KEY,
        ""ProductVersion"" TEXT NOT NULL
    );";
    createHistory.ExecuteNonQuery();
}

using (var check = conn.CreateCommand())
{
    check.CommandText = "SELECT COUNT(*) FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" = $id;";
    check.Parameters.AddWithValue("$id", migrationId);
    var count = (long)check.ExecuteScalar()!;

    if (count > 0)
    {
        Console.WriteLine($"Migration '{migrationId}' is already marked as applied. Nothing to do.");
        return 0;
    }
}

using (var insert = conn.CreateCommand())
{
    insert.CommandText = "INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ($id, $version);";
    insert.Parameters.AddWithValue("$id", migrationId);
    insert.Parameters.AddWithValue("$version", productVersion);
    insert.ExecuteNonQuery();
}

Console.WriteLine($"Marked migration '{migrationId}' as applied.");
return 0;
