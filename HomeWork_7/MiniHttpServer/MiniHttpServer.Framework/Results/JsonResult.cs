using System.Text.Json;

namespace MyHttpServer.Framework.Results;

public sealed class JsonResult(object data, int statusCode = 200) : IHttpResult
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public int StatusCode { get; } = statusCode;
    public string ContentType => "application/json; charset=utf-8";
    public string Render() => JsonSerializer.Serialize(data, Options);
}
