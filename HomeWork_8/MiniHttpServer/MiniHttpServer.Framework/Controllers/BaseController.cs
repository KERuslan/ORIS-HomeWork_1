using MyHttpServer.Framework.Context;
using MyHttpServer.Framework.Results;

namespace MyHttpServer.Framework.Controllers;

public abstract class BaseController
{
    public RequestContext Context { get; internal set; } = null!;

    protected IHttpResult Html(string html, int statusCode = 200) => new HtmlResult(html, statusCode);
    protected IHttpResult Json(object data, int statusCode = 200) => new JsonResult(data, statusCode);
}
