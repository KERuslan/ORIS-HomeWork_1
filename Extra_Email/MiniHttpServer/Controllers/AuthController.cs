using System.Net;
using System.Net.Mail;
using System.Web;
using MyHttpServer.Framework.Handlers;
using MyHttpServer.Services;

namespace MyHttpServer.Controllers;

public sealed class AuthController(ResponseWriter writer, IEmailService emailService)
{
    public async Task SendEmail(HttpListenerContext context)
    {
        var request = context.Request;
        string? mediaType = request.ContentType?.Split(';', 2)[0].Trim();
        if (!string.Equals(mediaType, "application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
        {
            await writer.SendTextAsync(context, "Ожидается HTML-форма.", "text/plain; charset=utf-8", 400);
            return;
        }

        using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
        var form = HttpUtility.ParseQueryString(await reader.ReadToEndAsync());
        string? demoUser = form["demoUser"]?.Trim();
        string? message = form["message"]?.Trim();
        if (string.IsNullOrEmpty(demoUser) || demoUser.Length > 80 ||
            string.IsNullOrEmpty(message) || message.Length > 400)
        {
            await writer.SendTextAsync(context, "Проверьте тестовое имя и сообщение.", "text/plain; charset=utf-8", 400);
            return;
        }

        try
        {
            string address = Environment.GetEnvironmentVariable("MAIL_EMAIL")
                ?? throw new InvalidOperationException("MAIL_EMAIL is not set.");
            using var mail = new MailMessage(address, address)
            {
                Subject = "Учебное сообщение ОРИС",
                Body = $"Тестовое имя: {demoUser}\nСообщение: {message}"
            };
            await emailService.SendAsync(mail);
            await writer.SendTextAsync(context, "Тестовое письмо отправлено.", "text/plain; charset=utf-8");
        }
        catch (Exception e) when (e is SmtpException or FormatException or InvalidOperationException)
        {
            await writer.SendTextAsync(context, "Проверьте настройки учебной почты.", "text/plain; charset=utf-8", 500);
        }
    }
}
