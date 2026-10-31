using System.Net;
using System.Net.Mail;

namespace MyHttpServer.Controllers;

/// <summary>
/// Sends email messages through an SMTP server.
/// </summary>
public sealed class SmtpEmailSender : IEmailSender
{
    /// <summary>
    /// Sends a message using the configured SMTP account.
    /// </summary>
    public async Task SendAsync(MailMessage message)
    {
        // Read the sender address from the environment.
        string email = Environment.GetEnvironmentVariable("MAIL_EMAIL") ?? throw new InvalidOperationException("MAIL_EMAIL is not set.");
        // Read the sender password from the environment.
        string password = Environment.GetEnvironmentVariable("MAIL_PASSWORD") ?? throw new InvalidOperationException("MAIL_PASSWORD is not set.");
        // Create the SMTP client.
        using var smtp = new SmtpClient("smtp.yandex.ru", 587);
        // Set the account details.
        smtp.Credentials = new NetworkCredential(email, password);
        // Turn on SSL protection.
        smtp.EnableSsl = true;
        // Send the message without blocking the current thread.
        await smtp.SendMailAsync(message);
    }
}
