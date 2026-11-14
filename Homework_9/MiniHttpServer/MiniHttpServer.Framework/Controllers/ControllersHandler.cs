using System.Net;
using MiniHttpServer.Framework.Results;

namespace MyHttpServer.Core.Handlers;

public sealed class ControllersHandler(ResponseWriter writer) : IRequestHandler
{
    private readonly Dictionary<string, Func<Task<IHttpResult>>> _getRoutes = new(StringComparer.OrdinalIgnoreCase);

    public void MapGet(string path, Func<Task<IHttpResult>> action) => _getRoutes.Add(path, action);

    public async Task<bool> HandleAsync(HttpListenerContext context)
    {
        string? path = context.Request.Url?.AbsolutePath;
        if (context.Request.HttpMethod is not ("GET" or "HEAD") || path is null
            || !_getRoutes.TryGetValue(path, out var action))
            return false;

        IHttpResult result = await action();
        await result.ExecuteAsync(context, writer);
        return true;
    }
}
