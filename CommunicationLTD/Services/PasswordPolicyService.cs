using CommunicationLTD.Models;
using CommunicationLTD.Options;
using Microsoft.Extensions.Options;

namespace CommunicationLTD.Services;

public class PasswordPolicyService
{
    private readonly PasswordPolicyOptions _options;
    private readonly IPasswordHasher _passwordHasher;

    public PasswordPolicyService(IOptions<PasswordPolicyOptions> options, IPasswordHasher passwordHasher)
    {
        _options = options.Value;
        _passwordHasher = passwordHasher;
    }

    public int HistoryLimit => _options.PasswordHistoryLimit;
    public int LoginAttemptLimit => _options.LoginAttemptLimit;

    public bool Validate(string password, out List<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrWhiteSpace(password))
        {
            errors.Add("Password is required.");
            return false;
        }

        if (password.Length < _options.MinimumLength)
        {
            errors.Add($"Password must be at least {_options.MinimumLength} characters long.");
        }

        if (_options.RequireUppercase && !password.Any(char.IsUpper))
        {
            errors.Add("Password must contain at least one uppercase letter.");
        }

        if (_options.RequireLowercase && !password.Any(char.IsLower))
        {
            errors.Add("Password must contain at least one lowercase letter.");
        }

        if (_options.RequireDigit && !password.Any(char.IsDigit))
        {
            errors.Add("Password must contain at least one digit.");
        }

        if (_options.RequireSpecialCharacter && !password.Any(ch => !char.IsLetterOrDigit(ch)))
        {
            errors.Add("Password must contain at least one special character.");
        }

        if (_options.DisallowCommonPasswords && IsCommonPassword(password))
        {
            errors.Add("Password is too common.");
        }

        return errors.Count == 0;
    }

    // Same as Validate, but also rejects the password if it matches one of the
    // user's last PasswordHistoryLimit passwords.
    public bool Validate(string password, IEnumerable<PasswordHistory> history, out List<string> errors)
    {
        var isValid = Validate(password, out errors);

        if (_options.PasswordHistoryLimit > 0)
        {
            var recentHistory = history
                .OrderByDescending(h => h.CreatedAt)
                .Take(_options.PasswordHistoryLimit);

            foreach (var entry in recentHistory)
            {
                if (_passwordHasher.Verify(password, entry.PasswordHash, entry.PasswordSalt))
                {
                    errors.Add($"You cannot reuse any of your last {_options.PasswordHistoryLimit} passwords.");
                    isValid = false;
                    break;
                }
            }
        }

        return isValid;
    }

    private static bool IsCommonPassword(string password)
    {
        var normalized = password.Trim().ToLowerInvariant();
        var commonPasswords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "password",
            "password123",
            "12345678",
            "qwerty123",
            "welcome123",
            "letmein",
            "admin123",
            "sunshine"
        };

        return commonPasswords.Contains(normalized);
    }
}
