using System.Net;

namespace MyHttpServer.Framework.Context;

public sealed record RequestContext(string Method, string Path, string? UserAgent)
{
    public static RequestContext From(HttpListenerRequest request) =>
        new(request.HttpMethod, request.Url?.AbsolutePath ?? "/", request.UserAgent);
}
