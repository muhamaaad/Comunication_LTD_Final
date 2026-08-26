using CommunicationLTD.Options;
using CommunicationLTD.Services;
using Microsoft.Extensions.Options;

var options = Options.Create(new PasswordPolicyOptions
{
    MinimumLength = 12,
    RequireUppercase = true,
    RequireLowercase = true,
    RequireDigit = true,
    RequireSpecialCharacter = true,
    DisallowCommonPasswords = true
});

var service = new PasswordPolicyService(options);

var cases = new Dictionary<string, string>
{
    ["short1!"] = "short1!",
    ["alllowercase123!"] = "alllowercase123!",
    ["NoDigitsHere!"] = "NoDigitsHere!",
    ["NoSpecialChar123"] = "NoSpecialChar123",
    ["password"] = "password",
    ["Str0ng&SecurePass!"] = "Str0ng&SecurePass!"
};

foreach (var entry in cases)
{
    var valid = service.Validate(entry.Value, out var errors);
    Console.WriteLine($"Password: {entry.Key}");
    Console.WriteLine($"Valid: {valid}");
    Console.WriteLine($"Errors: {(errors.Count == 0 ? "<none>" : string.Join(" | ", errors))}");
    Console.WriteLine();
}
