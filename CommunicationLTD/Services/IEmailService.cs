namespace CommunicationLTD.Services;

public interface IEmailService
{
    Task SendConfirmationEmailAsync(string toEmail, string confirmationLink);

    Task SendPasswordResetCodeAsync(string toEmail, string resetCode);
}
