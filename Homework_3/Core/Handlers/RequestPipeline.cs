using System.Net;

namespace MyHttpServer.Core.Handlers;

public sealed class RequestPipeline
{
    private readonly IReadOnlyList<IRequestHandler> _handlers;

    public RequestPipeline(IEnumerable<IRequestHandler> handlers) => _handlers = handlers.ToList();

    public async Task ProcessAsync(HttpListenerContext context)
    {
        foreach (var handler in _handlers)
        {
            if (await handler.HandleAsync(context))
                return;
        }
    }
}
