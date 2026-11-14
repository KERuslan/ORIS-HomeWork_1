using System.Net;
using MyHttpServer.Core.Handlers;

namespace MiniHttpServer.Framework.Results;

public interface IHttpResult
{
    Task ExecuteAsync(HttpListenerContext context, ResponseWriter writer);
}
