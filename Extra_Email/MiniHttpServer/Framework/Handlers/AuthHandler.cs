using System.Net;
using MyHttpServer.Controllers;

namespace MyHttpServer.Framework.Handlers;

public sealed class AuthHandler : IRequestHandler
{
    private const string LoginPath = "/auth/sendemail";

    private readonly AuthController _controller;


    public AuthHandler(AuthController controller)
    {
        _controller = controller;

    }

    public async Task<bool> HandleAsync(HttpListenerContext context)
    {
        var request = context.Request;

        if (!string.Equals(request.Url?.LocalPath, LoginPath, StringComparison.OrdinalIgnoreCase))
            return false;

        if (request.HttpMethod != "POST")
            return false;

        await _controller.SendEmail(context);
        return true;
    }
}
