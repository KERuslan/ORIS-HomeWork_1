using System.Net;
using MiniHttpServer.HtmlTemplating;
using MyHttpServer.Core.Handlers;

namespace MiniHttpServer.Framework.Results;

public sealed class PageResult(string templatePath, object model, IHtmlTemplateRenderer renderer) : IHttpResult
{
    public async Task ExecuteAsync(HttpListenerContext context, ResponseWriter writer)
    {
        string template = await File.ReadAllTextAsync(templatePath);
        await writer.SendTextAsync(context, renderer.Render(template, model));
    }
}
