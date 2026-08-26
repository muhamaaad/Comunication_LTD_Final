using Microsoft.Data.Sqlite;
using System;

var dbPath = "communicationltd.db";
if (!System.IO.File.Exists(dbPath))
{
    Console.Error.WriteLine($"DB not found: {dbPath}");
    return 1;
}
using var conn = new SqliteConnection($"Data Source={dbPath}");
conn.Open();

Console.WriteLine("PRAGMA table_info('Users'):");
using (var cmd = conn.CreateCommand())
{
    cmd.CommandText = "PRAGMA table_info('Users');";
    using var rdr = cmd.ExecuteReader();
    while (rdr.Read())
    {
        Console.WriteLine($"cid={rdr.GetInt32(0)}, name={rdr.GetString(1)}, type={rdr.GetString(2)}, notnull={rdr.GetInt32(3)}, dflt_value={(rdr.IsDBNull(4)?"NULL":rdr.GetString(4))}, pk={rdr.GetInt32(5)}");
    }
}

Console.WriteLine();
Console.WriteLine("Indexes for Users from sqlite_master:");
using (var cmd = conn.CreateCommand())
{
    cmd.CommandText = "SELECT name, sql FROM sqlite_master WHERE type='index' AND tbl_name='Users'";
    using var rdr = cmd.ExecuteReader();
    while (rdr.Read())
    {
        var name = rdr.IsDBNull(0)?"":rdr.GetString(0);
        var sql = rdr.IsDBNull(1)?"":rdr.GetString(1);
        Console.WriteLine($"name={name}, sql={sql}");
    }
}

Console.WriteLine();
Console.WriteLine("Users rows:");
using (var cmd = conn.CreateCommand())
{
    cmd.CommandText = "SELECT Id, Email, length(PasswordHash) AS HashLength, length(PasswordSalt) AS SaltLength FROM Users ORDER BY Id;";
    using var rdr = cmd.ExecuteReader();
    while (rdr.Read())
    {
        Console.WriteLine($"Id={rdr.GetInt32(0)}, Email={rdr.GetString(1)}, HashLength={rdr.GetInt32(2)}, SaltLength={rdr.GetInt32(3)}");
    }
}

return 0;