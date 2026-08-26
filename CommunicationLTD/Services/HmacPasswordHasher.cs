using System.Security.Cryptography;
using System.Text;

namespace CommunicationLTD.Services;

public class HmacPasswordHasher : IPasswordHasher
{
    public (byte[] hash, byte[] salt) Hash(string password)
    {
        var salt = new byte[32];
        RandomNumberGenerator.Fill(salt);

        using var hmac = new HMACSHA256(salt);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

        return (hash, salt);
    }

    public bool Verify(string password, byte[] hash, byte[] salt)
    {
        if (hash is null || salt is null)
        {
            return false;
        }

        using var hmac = new HMACSHA256(salt);
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

        return CryptographicOperations.FixedTimeEquals(computedHash, hash);
    }
}
