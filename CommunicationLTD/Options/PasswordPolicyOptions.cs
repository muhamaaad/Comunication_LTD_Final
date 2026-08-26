namespace CommunicationLTD.Options;

public class PasswordPolicyOptions
{
    public int MinimumLength { get; set; } = 12;
    public bool RequireUppercase { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireDigit { get; set; } = true;
    public bool RequireSpecialCharacter { get; set; } = true;
    public bool DisallowCommonPasswords { get; set; } = true;

    // How many previous passwords a user may not reuse (0 disables the check).
    public int PasswordHistoryLimit { get; set; } = 3;

    // How many failed login attempts are allowed before the account is locked.
    public int LoginAttemptLimit { get; set; } = 3;
}
