using System.Net;
using System.Web;

namespace MyHttpServer.Core.Handlers;

public sealed class LoginHandler : IRequestHandler
{
    private const string LoginPath = "/login";

    private readonly string _loginFile;
    private readonly ResponseWriter _writer;

    public LoginHandler(string loginFile, ResponseWriter writer)
    {
        _loginFile = loginFile;
        _writer = writer;
    }

    public async Task<bool> HandleAsync(HttpListenerContext context)
    {
        var request = context.Request;

        if (!string.Equals(request.Url?.LocalPath, LoginPath, StringComparison.OrdinalIgnoreCase))
            return false;

        switch (request.HttpMethod)
        {
            case "GET":
            case "HEAD":
                await _writer.SendFileAsync(context, _loginFile);
                return true;

            case "POST":
                await HandlePostAsync(context);
                return true;

            default:
                return false;
        }
    }

    private async Task HandlePostAsync(HttpListenerContext context)
    {
        using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
        string body = await reader.ReadToEndAsync();

        var form = HttpUtility.ParseQueryString(body);

        Console.WriteLine($"login: {form["login"]}");
        Console.WriteLine($"password: {form["password"]}");

        await _writer.SendTextAsync(context, "<h1>Данные получены</h1>");
    }
}
