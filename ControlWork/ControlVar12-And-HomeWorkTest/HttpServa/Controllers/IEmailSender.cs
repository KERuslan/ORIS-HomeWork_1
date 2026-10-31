using System.Net.Mail;

namespace MyHttpServer.Controllers;

/// <summary>
/// Sends an email message.
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Sends the given email message.
    /// </summary>
    Task SendAsync(MailMessage message);
}
