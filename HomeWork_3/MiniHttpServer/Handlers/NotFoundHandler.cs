using System.Net;

namespace MyHttpServer.Core.Handlers;
public sealed class NotFoundHandler : IRequestHandler
{
    private readonly string _notFoundFile;
    private readonly ResponseWriter _writer;

    public NotFoundHandler(string notFoundFile, ResponseWriter writer)
    {
        _notFoundFile = notFoundFile;
        _writer = writer;
    }

    public async Task<bool> HandleAsync(HttpListenerContext context)
    {
        if (File.Exists(_notFoundFile))
            await _writer.SendFileAsync(context, _notFoundFile, 404);
        else
            await _writer.SendTextAsync(context, "404 Not Found", "text/plain; charset=utf-8", 404);

        return true;
    }
}
