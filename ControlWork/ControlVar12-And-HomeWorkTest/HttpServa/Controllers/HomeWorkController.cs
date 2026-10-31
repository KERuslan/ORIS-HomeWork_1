using System.IO.Compression;
using System.Net;
using System.Net.Mail;
using MyHttpServer.Core.Handlers;

namespace MyHttpServer.Controllers;

public sealed class HomeWorkController
{
    private const string GroupNumber = "11-541";
    private const string FullName = "Комаров Руслан Евгеньевич";
    private const string SenderEmail = "komaroffr@yandex.ru";
    private readonly ResponseWriter _writer;
    private readonly IEmailSender _emailSender;
    private readonly Func<Stream> _archiveFactory;

    public HomeWorkController(ResponseWriter writer)
        : this(writer, new SmtpEmailSender(), CreateProjectZip)
    {
    }

    public HomeWorkController(ResponseWriter writer, IEmailSender emailSender, Func<Stream> archiveFactory)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
        _archiveFactory = archiveFactory ?? throw new ArgumentNullException(nameof(archiveFactory));
    }

    public async Task Send(HttpListenerContext context)
    {
        string? to = context.Request.QueryString["to"];
        if (string.IsNullOrWhiteSpace(to))
        {
            await _writer.SendTextAsync(context, "<h2>Не указан параметр to</h2>", statusCode: 400);
            return;
        }

        try
        {
            MailAddress from = new MailAddress(SenderEmail, "HomeWorkORIS");
            MailAddress toAddress = new MailAddress(to);
            using MailMessage message = new MailMessage(from, toAddress);
            message.Subject = "Домашняя работа";
            message.Body = $"<h2>Группа: {GroupNumber}</h2><h2>ФИО: {FullName}</h2>";
            message.IsBodyHtml = true;
            using Stream archive = _archiveFactory();
            message.Attachments.Add(new Attachment(archive, "HttpMyServer.zip", "application/zip"));
            await _emailSender.SendAsync(message);
            await _writer.SendTextAsync(context, "<h2>Письмо отправлено</h2>");
        }
        catch (Exception e) when (e is FormatException or SmtpException or InvalidOperationException)
        {
            Console.WriteLine($"[MAIL] {e.Message}");
            await _writer.SendTextAsync(context, "<h2>Не удалось отправить письмо</h2>", statusCode: 500);
        }
    }

    private static MemoryStream CreateProjectZip()
    {
        string root = FindProjectRoot();
        string[] skip = { "bin", "obj", ".idea", ".vs" };
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (string file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            {
                string relative = Path.GetRelativePath(root, file);
                string[] parts = relative.Split(Path.DirectorySeparatorChar);
                if (parts.Take(parts.Length - 1).Any(p => skip.Contains(p))) continue;
                archive.CreateEntryFromFile(file, relative.Replace('\\', '/'));
            }
        }
        stream.Position = 0;
        return stream;
    }

    private static string FindProjectRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (dir.GetFiles("*.csproj").Length > 0) return dir.FullName;
            dir = dir.Parent;
        }
        return AppContext.BaseDirectory;
    }
}
