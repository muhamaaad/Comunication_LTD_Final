namespace CommunicationLTD.Options;

public class EmailOptions
{
    public string SmtpServer { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string SenderAddress { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public bool UseSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
