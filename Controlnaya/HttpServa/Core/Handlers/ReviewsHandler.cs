using System.Net;
using MyHttpServer.Controllers;

namespace MyHttpServer.Core.Handlers;

public sealed class ReviewsHandler : IRequestHandler
{
    private readonly ReviewsController _controller;

    public ReviewsHandler(ReviewsController controller) => _controller = controller;

    public async Task<bool> HandleAsync(HttpListenerContext context)
    {
        string path = context.Request.Url?.LocalPath ?? "/";
        string method = context.Request.HttpMethod;

        if (path == "/reviews" && method == "GET")
            await _controller.List(context);
        else if (path == "/reviews/details" && method == "GET")
            await _controller.Details(context);
        else if (path == "/reviews/create" && method == "POST")
            await _controller.Create(context);
        else if (path == "/reviews/update" && method == "PUT")
            await _controller.Update(context);
        else if (path is "/reviews" or "/reviews/details" or "/reviews/create" or "/reviews/update")
        {
            string allow = path switch
            {
                "/reviews" or "/reviews/details" => "GET",
                "/reviews/create" => "POST",
                _ => "PUT"
            };
            await _controller.MethodNotAllowed(context, allow);
        }
        else
            return false;

        return true;
    }
}
