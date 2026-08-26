using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;

// Seeds (or updates) one pre-confirmed test user directly into communicationltd.db,
// using the exact same HMAC-SHA256 + salt scheme as Services/HmacPasswordHasher.cs.
// Run from the CommunicationLTD project folder (same place the .db file lives).

var dbPath = args.Length > 0 ? args[0] : Path.Combine("..", "..", "communicationltd.db");
if (!File.Exists(dbPath))
{
    Console.Error.WriteLine($"DB not found at '{dbPath}' (resolved: '{Path.GetFullPath(dbPath)}'). Pass the path to communicationltd.db as an argument, e.g.: dotnet run -- ../../communicationltd.db");
    return 1;
}

var email = "finaltest@gmail.com"; // stored normalized/lowercase, same as AccountController does
var password = "zxcvbn346512$#A";

var salt = new byte[32];
RandomNumberGenerator.Fill(salt);
using var hmac = new HMACSHA256(salt);
var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

using var conn = new SqliteConnection($"Data Source={dbPath}");
conn.Open();

int userId;
using (var check = conn.CreateCommand())
{
    check.CommandText = "SELECT Id FROM Users WHERE Email = $email;";
    check.Parameters.AddWithValue("$email", email);
    var existingId = check.ExecuteScalar();

    if (existingId is not null)
    {
        userId = Convert.ToInt32(existingId);

        using var update = conn.CreateCommand();
        update.CommandText = @"UPDATE Users
            SET PasswordHash = $hash, PasswordSalt = $salt, EmailConfirmed = 1, ConfirmationToken = NULL, IsAdmin = 1,
                IsLocked = 0, LockedUntil = NULL, LoginAttempts = 0
            WHERE Id = $id;";
        update.Parameters.AddWithValue("$hash", hash);
        update.Parameters.AddWithValue("$salt", salt);
        update.Parameters.AddWithValue("$id", userId);
        update.ExecuteNonQuery();
        Console.WriteLine($"Updated existing user Id={userId} ({email}) with new password, confirmed email, admin role, and cleared any lockout.");
    }
    else
    {
        using var insert = conn.CreateCommand();
        insert.CommandText = @"INSERT INTO Users (Email, PasswordHash, PasswordSalt, CreatedAt, EmailConfirmed, ConfirmationToken, IsAdmin)
            VALUES ($email, $hash, $salt, $createdAt, 1, NULL, 1);
            SELECT last_insert_rowid();";
        insert.Parameters.AddWithValue("$email", email);
        insert.Parameters.AddWithValue("$hash", hash);
        insert.Parameters.AddWithValue("$salt", salt);
        insert.Parameters.AddWithValue("$createdAt", DateTime.UtcNow.ToString("o"));
        userId = Convert.ToInt32(insert.ExecuteScalar());
        Console.WriteLine($"Seeded test user (admin): {email}");
    }
}

using (var insertHistory = conn.CreateCommand())
{
    insertHistory.CommandText = @"INSERT INTO PasswordHistories (UserId, PasswordHash, PasswordSalt, CreatedAt)
        VALUES ($userId, $hash, $salt, $createdAt);";
    insertHistory.Parameters.AddWithValue("$userId", userId);
    insertHistory.Parameters.AddWithValue("$hash", hash);
    insertHistory.Parameters.AddWithValue("$salt", salt);
    insertHistory.Parameters.AddWithValue("$createdAt", DateTime.UtcNow.ToString("o"));
    insertHistory.ExecuteNonQuery();
}

// Also seed a matching Customer row (used by AccountController.Profile to show customer details after login).
using (var checkCustomer = conn.CreateCommand())
{
    checkCustomer.CommandText = "SELECT Id FROM Customers WHERE Email = $email;";
    checkCustomer.Parameters.AddWithValue("$email", email);
    var existingCustomerId = checkCustomer.ExecuteScalar();

    if (existingCustomerId is null)
    {
        using var insertCustomer = conn.CreateCommand();
        insertCustomer.CommandText = @"INSERT INTO Customers (FullName, Email, Phone, SectorId, PackageId)
            VALUES ($fullName, $email, $phone, $sectorId, $packageId);";
        insertCustomer.Parameters.AddWithValue("$fullName", "Final Test");
        insertCustomer.Parameters.AddWithValue("$email", email);
        insertCustomer.Parameters.AddWithValue("$phone", "050-0000000");
        insertCustomer.Parameters.AddWithValue("$sectorId", 1);
        insertCustomer.Parameters.AddWithValue("$packageId", 1);
        insertCustomer.ExecuteNonQuery();
        Console.WriteLine($"Seeded matching customer record for: {email}");
    }
    else
    {
        Console.WriteLine($"Customer record already exists (Id={existingCustomerId}) for: {email}");
    }
}

return 0;
