using System.Net;
using System.Text;
using MyHttpServer.Core.ContentTypes;

namespace MyHttpServer.Core.Handlers;

public sealed class ResponseWriter
{
    private readonly IContentTypeResolver _contentTypes;

    public ResponseWriter(IContentTypeResolver contentTypes) => _contentTypes = contentTypes;

    public async Task SendFileAsync(HttpListenerContext context, string filePath, int statusCode = 200)
    {
        var response = context.Response;
        var info = new FileInfo(filePath);

        response.StatusCode = statusCode;
        response.ContentType = _contentTypes.Resolve(filePath);
        response.ContentLength64 = info.Length;
        response.Headers["X-Content-Type-Options"] = "nosniff";

        if (IsHead(context)) return;

        await using var file = new FileStream(filePath, FileMode.Open, FileAccess.Read,
                                              FileShare.Read, 81920, useAsync: true);
        await file.CopyToAsync(response.OutputStream);
    }

    public async Task SendTextAsync(HttpListenerContext context, string text,
                                    string contentType = "text/html; charset=utf-8", int statusCode = 200)
    {
        var response = context.Response;
        byte[] buffer = Encoding.UTF8.GetBytes(text);

        response.StatusCode = statusCode;
        response.ContentType = contentType;
        response.ContentLength64 = buffer.Length;

        if (IsHead(context)) return;

        await response.OutputStream.WriteAsync(buffer);
    }

    public void Redirect(HttpListenerContext context, string location)
    {
        context.Response.StatusCode = 301;
        context.Response.RedirectLocation = location;
        context.Response.ContentLength64 = 0;
    }

    private static bool IsHead(HttpListenerContext context) =>
        context.Request.HttpMethod.Equals("HEAD", StringComparison.OrdinalIgnoreCase);
}
