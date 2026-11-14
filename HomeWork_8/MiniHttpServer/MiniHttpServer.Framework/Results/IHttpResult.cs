namespace MyHttpServer.Framework.Results;

public interface IHttpResult
{
    int StatusCode { get; }
    string ContentType { get; }
    string Render();
}
