using System.Net;
using MyHttpServer.Controllers;

namespace MyHttpServer.Core.Handlers;

public sealed class HomeWorkHandler : IRequestHandler
{
    private const string SendPath = "/home-work/send";

    private readonly HomeWorkController _controller;

    public HomeWorkHandler(HomeWorkController controller) => _controller = controller;

    public async Task<bool> HandleAsync(HttpListenerContext context)
    {
        var request = context.Request;

        if (request.HttpMethod != "GET" ||
            !string.Equals(request.Url?.LocalPath, SendPath, StringComparison.OrdinalIgnoreCase))
            return false;

        await _controller.Send(context);
        return true;
    }
}
