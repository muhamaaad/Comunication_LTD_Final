using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CommunicationLTD.Data;
using CommunicationLTD.Models;
using CommunicationLTD.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunicationLTD.Controllers;

public class AccountController : Controller
{
    private readonly CommunicationLtdDbContext _context;
    private readonly PasswordPolicyService _passwordPolicyService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailService _emailService;

    public AccountController(
        CommunicationLtdDbContext context,
        PasswordPolicyService passwordPolicyService,
        IPasswordHasher passwordHasher,
        IEmailService emailService)
    {
        _context = context;
        _passwordPolicyService = passwordPolicyService;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
    }

    public IActionResult Login()
    {
        if (TempData["SuccessMessage"] is string successMessage)
        {
            ViewData["SuccessMessage"] = successMessage;
        }

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError(string.Empty, "Please enter both email and password.");
            return View();
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "No account exists with this email address.");
            return View();
        }

        if (user.IsLocked)
        {
            ModelState.AddModelError(string.Empty, "This account is locked due to too many failed login attempts. Use \"Forgot password\" to regain access.");
            return View();
        }

        if (!_passwordHasher.Verify(password, user.PasswordHash, user.PasswordSalt))
        {
            user.LoginAttempts++;
            var remainingAttempts = _passwordPolicyService.LoginAttemptLimit - user.LoginAttempts;

            if (remainingAttempts <= 0)
            {
                user.IsLocked = true;
                user.LockedUntil = DateTime.UtcNow.AddMinutes(15);
                await _context.SaveChangesAsync();
                ModelState.AddModelError(string.Empty, "Account locked due to too many failed login attempts.");
            }
            else
            {
                await _context.SaveChangesAsync();
                ModelState.AddModelError(string.Empty, $"Incorrect password. {remainingAttempts} attempt(s) remaining.");
            }

            return View();
        }

        if (!user.EmailConfirmed)
        {
            ModelState.AddModelError(string.Empty, "Please confirm your email address before signing in.");
            return View();
        }

        if (user.LoginAttempts > 0)
        {
            user.LoginAttempts = 0;
            await _context.SaveChangesAsync();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email)
        };
        if (user.IsAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        }
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        return RedirectToAction(nameof(Profile));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            ModelState.AddModelError(string.Empty, "Please enter your email address.");
            return View();
        }

        var normalizedEmail = identifier.Trim().ToLowerInvariant();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        // Always show the same success message regardless of whether the account exists,
        // so this screen cannot be used to enumerate registered emails.
        if (user is not null)
        {
            var resetToken = new PasswordResetToken
            {
                UserId = user.Id,
                ResetToken = ResetCodeGenerator.GenerateSha1Code(),
                Expiration = DateTime.UtcNow.AddMinutes(15),
                Used = false
            };

            _context.PasswordResetTokens.Add(resetToken);
            await _context.SaveChangesAsync();

            await _emailService.SendPasswordResetCodeAsync(user.Email, resetToken.ResetToken);
        }

        TempData["SuccessMessage"] = "If an account exists with this email, a reset code has been sent.";
        return RedirectToAction(nameof(VerifyResetCode));
    }

    [HttpGet]
    public IActionResult VerifyResetCode()
    {
        if (TempData["SuccessMessage"] is string successMessage)
        {
            ViewData["SuccessMessage"] = successMessage;
            TempData.Keep(nameof(successMessage));
        }

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyResetCode(string resetCode)
    {
        if (string.IsNullOrWhiteSpace(resetCode))
        {
            ModelState.AddModelError(string.Empty, "Please enter the verification code.");
            return View();
        }

        var normalizedCode = resetCode.Trim().ToLowerInvariant();
        var token = await _context.PasswordResetTokens
            .FirstOrDefaultAsync(t => t.ResetToken == normalizedCode && !t.Used);

        if (token is null || token.Expiration < DateTime.UtcNow)
        {
            ModelState.AddModelError(string.Empty, "Invalid or expired verification code.");
            return View();
        }

        HttpContext.Session.SetInt32("ResetUserId", token.UserId);
        HttpContext.Session.SetInt32("ResetTokenId", token.Id);

        return RedirectToAction(nameof(ResetPassword));
    }

    [HttpGet]
    public IActionResult ResetPassword()
    {
        if (HttpContext.Session.GetInt32("ResetUserId") is null)
        {
            return RedirectToAction(nameof(ForgotPassword));
        }

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(string newPassword, string confirmPassword)
    {
        var userId = HttpContext.Session.GetInt32("ResetUserId");
        var tokenId = HttpContext.Session.GetInt32("ResetTokenId");

        if (userId is null || tokenId is null)
        {
            return RedirectToAction(nameof(ForgotPassword));
        }

        if (string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword) ||
            !string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
        {
            ModelState.AddModelError(string.Empty, "Passwords do not match.");
            return View();
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId.Value);
        var token = await _context.PasswordResetTokens.FirstOrDefaultAsync(t => t.Id == tokenId.Value && !t.Used);

        if (user is null || token is null || token.Expiration < DateTime.UtcNow)
        {
            ModelState.AddModelError(string.Empty, "Password reset session is invalid or expired.");
            return View();
        }

        var history = await _context.PasswordHistories
            .Where(h => h.UserId == user.Id)
            .OrderByDescending(h => h.CreatedAt)
            .Take(_passwordPolicyService.HistoryLimit)
            .ToListAsync();

        if (!_passwordPolicyService.Validate(newPassword, history, out var policyErrors))
        {
            foreach (var error in policyErrors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return View();
        }

        _context.PasswordHistories.Add(new PasswordHistory
        {
            UserId = user.Id,
            PasswordHash = user.PasswordHash,
            PasswordSalt = user.PasswordSalt,
            CreatedAt = DateTime.UtcNow
        });

        var (hash, salt) = _passwordHasher.Hash(newPassword);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;
        token.Used = true;

        // A successful reset is also the standard way to recover a locked-out account.
        user.IsLocked = false;
        user.LoginAttempts = 0;
        user.LockedUntil = null;

        await _context.SaveChangesAsync();

        HttpContext.Session.Remove("ResetUserId");
        HttpContext.Session.Remove("ResetTokenId");

        TempData["SuccessMessage"] = "Password reset successfully. Please sign in.";
        return RedirectToAction(nameof(Login));
    }

    [Authorize]
    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View();
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(email))
        {
            return RedirectToAction(nameof(Login));
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user is null)
        {
            return RedirectToAction(nameof(Login));
        }

        if (string.IsNullOrWhiteSpace(currentPassword) || !_passwordHasher.Verify(currentPassword, user.PasswordHash, user.PasswordSalt))
        {
            ModelState.AddModelError(string.Empty, "Current password is incorrect.");
            return View();
        }

        if (string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword) ||
            !string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
        {
            ModelState.AddModelError(string.Empty, "New passwords do not match.");
            return View();
        }

        var history = await _context.PasswordHistories
            .Where(h => h.UserId == user.Id)
            .OrderByDescending(h => h.CreatedAt)
            .Take(_passwordPolicyService.HistoryLimit)
            .ToListAsync();

        if (!_passwordPolicyService.Validate(newPassword, history, out var policyErrors))
        {
            foreach (var error in policyErrors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return View();
        }

        // Keep the password being replaced in history before overwriting it.
        _context.PasswordHistories.Add(new PasswordHistory
        {
            UserId = user.Id,
            PasswordHash = user.PasswordHash,
            PasswordSalt = user.PasswordSalt,
            CreatedAt = DateTime.UtcNow
        });

        var (hash, salt) = _passwordHasher.Hash(newPassword);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Password changed successfully.";
        return RedirectToAction(nameof(Profile));
    }

    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(email))
        {
            return RedirectToAction(nameof(Login));
        }

        if (TempData["SuccessMessage"] is string successMessage)
        {
            ViewData["SuccessMessage"] = successMessage;
        }

        var customer = await _context.Customers
            .Include(c => c.Sector)
            .Include(c => c.InternetPackage)
            .FirstOrDefaultAsync(c => c.Email == email);

        return View(customer);
    }

    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(string fullName, string email, string password, string confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(email) || !MailAddress.TryCreate(email.Trim(), out _))
        {
            ModelState.AddModelError(nameof(email), "Please enter a valid email address.");
        }

        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(confirmPassword) || !string.Equals(password, confirmPassword, StringComparison.Ordinal))
        {
            ModelState.AddModelError(nameof(confirmPassword), "Passwords do not match.");
        }

        if (!ModelState.IsValid)
        {
            return View();
        }

        if (!_passwordPolicyService.Validate(password, out var policyErrors))
        {
            foreach (var error in policyErrors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return View();
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        if (_context.Users.Any(u => u.Email == normalizedEmail))
        {
            ModelState.AddModelError(string.Empty, "Registration could not be completed with the provided details.");
            return View();
        }

        var (hash, salt) = _passwordHasher.Hash(password);
        var confirmationToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var user = new User
        {
            Email = normalizedEmail,
            PasswordHash = hash,
            PasswordSalt = salt,
            CreatedAt = DateTime.UtcNow,
            EmailConfirmed = false,
            ConfirmationToken = confirmationToken
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Seed password history with the initial password so a later password
        // change correctly refuses to reuse it.
        _context.PasswordHistories.Add(new PasswordHistory
        {
            UserId = user.Id,
            PasswordHash = hash,
            PasswordSalt = salt,
            CreatedAt = user.CreatedAt
        });
        await _context.SaveChangesAsync();

        var confirmationLink = Url.Action(nameof(ConfirmEmail), "Account", new { token = confirmationToken }, Request.Scheme, Request.Host.Value);
        await _emailService.SendConfirmationEmailAsync(normalizedEmail, confirmationLink ?? string.Empty);

        TempData["SuccessMessage"] = "Registration was successful. Please sign in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return View("Error");
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.ConfirmationToken == token);
        if (user is null)
        {
            return View("Error");
        }

        user.EmailConfirmed = true;
        user.ConfirmationToken = null;
        await _context.SaveChangesAsync();

        return View();
    }
}
