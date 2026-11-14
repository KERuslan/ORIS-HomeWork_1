using System.Net.Mail;

namespace MyHttpServer.Services;

/// <summary>
/// Sends an email message.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends the given email message.
    /// </summary>
    Task SendAsync(MailMessage message);
}
