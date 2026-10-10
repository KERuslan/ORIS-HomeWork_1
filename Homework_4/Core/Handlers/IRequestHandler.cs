using System.Net;

namespace MyHttpServer.Core.Handlers;

public interface IRequestHandler
{
    Task<bool> HandleAsync(HttpListenerContext context);
}
