using System;
using System.ComponentModel.DataAnnotations;

namespace CommunicationLTD.Models;

public class User
{
    public int Id { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public byte[] PasswordHash { get; set; } = Array.Empty<byte>();

    [Required]
    public byte[] PasswordSalt { get; set; } = Array.Empty<byte>();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool EmailConfirmed { get; set; } = false;

    public string? ConfirmationToken { get; set; }

    public bool IsAdmin { get; set; } = false;

    public int LoginAttempts { get; set; } = 0;

    public bool IsLocked { get; set; } = false;

    public DateTime? LockedUntil { get; set; }
}
