using System.Collections.Specialized;
using System.Net;
using MyHttpServer.Framework.Routing;
using MyHttpServer.Framework.Context;
using MyHttpServer.Framework.Controllers;
using MyHttpServer.Framework.Results;

namespace MyHttpServer.Framework.Handlers;

public sealed class ControllersHandler(ControllerRoutes routes, ResponseWriter writer) : RequestHandler
{
    private const string PlainText = "text/plain; charset=utf-8";

    public override async Task<bool> HandleAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var route = routes.Find(request.HttpMethod, request.Url?.AbsolutePath ?? "/");
        if (route is null) return false;

        NameValueCollection values = request.QueryString;
        if (request.HttpMethod == "POST" && route.Action.GetParameters().Length > 0)
        {
            string? mediaType = request.ContentType?.Split(';', 2)[0].Trim();
            if (!string.Equals(mediaType, "application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
            {
                await writer.SendTextAsync(context, "Ожидается HTML-форма.", PlainText, 400);
                return true;
            }

            using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
            values = RequestParameters.ParseForm(await reader.ReadToEndAsync());
        }

        if (!RequestParameters.TryBind(route.Action.GetParameters(), values, out var arguments))
        {
            await writer.SendTextAsync(context, "Проверьте параметры запроса.", PlainText, 400);
            return true;
        }

        string body;
        string contentType;
        int statusCode;
        try
        {
            object controller = Activator.CreateInstance(route.Controller)!;
            if (controller is BaseController baseController)
                baseController.Context = RequestContext.From(request);
            object? result = route.Action.Invoke(controller, arguments);
            if (result is not IHttpResult httpResult)
                throw new InvalidOperationException("Ожидается HTTP-результат.");
            body = httpResult.Render();
            contentType = httpResult.ContentType;
            statusCode = httpResult.StatusCode;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error.GetBaseException().Message);
            body = "Внутренняя ошибка сервера.";
            contentType = PlainText;
            statusCode = 500;
        }

        await writer.SendTextAsync(context, body, contentType, statusCode);
        return true;
    }
}
