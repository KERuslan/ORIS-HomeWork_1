using System.Net;

namespace MyHttpServer.Core.Handlers;

public sealed class MethodGuardHandler : IRequestHandler
{
    private readonly ResponseWriter _writer;

    public MethodGuardHandler(ResponseWriter writer) => _writer = writer;

    public async Task<bool> HandleAsync(HttpListenerContext context)
    {
        string method = context.Request.HttpMethod;
        if (method is "GET" or "HEAD") return false;

        context.Response.Headers["Allow"] = "GET, HEAD";
        await _writer.SendTextAsync(context, "405 Method Not Allowed", "text/plain; charset=utf-8", 405);
        return true;
    }
}
