namespace MyHttpServer.Framework.Results;

public sealed class HtmlResult(string html, int statusCode = 200) : IHttpResult
{
    public int StatusCode { get; } = statusCode;
    public string ContentType => "text/html; charset=utf-8";
    public string Render() => html;
}
