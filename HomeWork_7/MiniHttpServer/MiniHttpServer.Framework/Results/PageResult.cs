using MyHttpServer.HtmlTemplating;

namespace MyHttpServer.Framework.Results;

public sealed class PageResult(
    IHtmlTemplateRenderer renderer, string templatePath, object model, int statusCode = 200) : IHttpResult
{
    public int StatusCode { get; } = statusCode;
    public string ContentType => "text/html; charset=utf-8";
    public string Render() => renderer.RenderFromFile(templatePath, model);
}
