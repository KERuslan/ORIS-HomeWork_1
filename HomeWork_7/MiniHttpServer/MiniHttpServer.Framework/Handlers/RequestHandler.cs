using System.Net;

namespace MyHttpServer.Framework.Handlers;

public abstract class RequestHandler : IRequestHandler
{
    public abstract Task<bool> HandleAsync(HttpListenerContext context);
}
