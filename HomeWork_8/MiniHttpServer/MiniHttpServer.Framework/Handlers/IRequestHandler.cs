using System.Net;

namespace MyHttpServer.Framework.Handlers;

public interface IRequestHandler
{
    Task<bool> HandleAsync(HttpListenerContext context);
}
