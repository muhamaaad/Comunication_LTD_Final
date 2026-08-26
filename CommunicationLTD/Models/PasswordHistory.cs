using System;

namespace CommunicationLTD.Models;

// Stores a user's previous password hashes so PasswordPolicyService can enforce
// "cannot reuse your last N passwords" (PasswordPolicyOptions.PasswordHistoryLimit).
public class PasswordHistory
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public byte[] PasswordHash { get; set; } = Array.Empty<byte>();
    public byte[] PasswordSalt { get; set; } = Array.Empty<byte>();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
