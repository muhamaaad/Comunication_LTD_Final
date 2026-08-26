using System.Net;
using System.Net.Mail;
using CommunicationLTD.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CommunicationLTD.Services;

public class SmtpEmailService : IEmailService
{
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<EmailOptions> options, ILogger<SmtpEmailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendConfirmationEmailAsync(string toEmail, string confirmationLink)
    {
        if (string.IsNullOrWhiteSpace(_options.SmtpServer) ||
            string.IsNullOrWhiteSpace(_options.SenderAddress) ||
            _options.SmtpServer.Equals("smtp.example.com", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("SMTP not configured; confirmation link for {Email}: {Link}", toEmail, confirmationLink);
            return;
        }

        using var client = new SmtpClient(_options.SmtpServer, _options.Port)
        {
            Credentials = new NetworkCredential(_options.Username, _options.Password),
            EnableSsl = _options.UseSsl
        };

        using var message = new MailMessage
        {
            From = new MailAddress(_options.SenderAddress, _options.SenderName),
            Subject = "Confirm your email",
            Body = $"Please confirm your account by clicking: {confirmationLink}",
            IsBodyHtml = false
        };

        message.To.Add(toEmail);
        await client.SendMailAsync(message);
    }

    public async Task SendPasswordResetCodeAsync(string toEmail, string resetCode)
    {
        if (string.IsNullOrWhiteSpace(_options.SmtpServer) ||
            string.IsNullOrWhiteSpace(_options.SenderAddress) ||
            _options.SmtpServer.Equals("smtp.example.com", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("SMTP not configured; password reset code for {Email}: {Code}", toEmail, resetCode);
            return;
        }

        using var client = new SmtpClient(_options.SmtpServer, _options.Port)
        {
            Credentials = new NetworkCredential(_options.Username, _options.Password),
            EnableSsl = _options.UseSsl
        };

        using var message = new MailMessage
        {
            From = new MailAddress(_options.SenderAddress, _options.SenderName),
            Subject = "Your password reset code",
            Body = $"Your password reset code is: {resetCode}\n\nThis code expires in 15 minutes. If you did not request a password reset, you can ignore this email.",
            IsBodyHtml = false
        };

        message.To.Add(toEmail);
        await client.SendMailAsync(message);
    }
}
