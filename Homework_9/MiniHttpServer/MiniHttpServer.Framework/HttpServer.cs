using System.Net;
using MyHttpServer.Core.Handlers;

namespace MyHttpServer.Core;

public sealed class HttpServer
{
    private readonly HttpListener _listener = new();
    private readonly RequestPipeline _pipeline;

    public string BaseUrl { get; }

    public HttpServer(string baseUrl, RequestPipeline pipeline)
    {
        _pipeline = pipeline;
        BaseUrl = baseUrl;
        _listener.Prefixes.Add(BaseUrl);
    }

    public void Start()
    {
        _listener.Start();
        Console.WriteLine($"Сервер начал свою работу: {BaseUrl}");
        _ = ListenLoopAsync();
    }

    public void Stop()
    {
        if (_listener.IsListening)
            _listener.Stop();
        _listener.Close();
        Console.WriteLine("Сервер завершил свою работу");
    }

    private async Task ListenLoopAsync()
    {
        while (_listener.IsListening)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync();
            }
            catch (Exception e) when (e is HttpListenerException or ObjectDisposedException or InvalidOperationException)
            {
                break;
            }

            _ = HandleSafeAsync(context);
        }
    }

    private async Task HandleSafeAsync(HttpListenerContext context)
    {
        try
        {
            await _pipeline.ProcessAsync(context);
            Console.WriteLine($"[{context.Request.HttpMethod}] {context.Request.Url?.PathAndQuery} -> {context.Response.StatusCode}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"[ERR] {context.Request.Url?.PathAndQuery}: {e.Message}");
            try { context.Response.StatusCode = 500; } catch { /* ответ наыучат */ }
        }
        finally
        {
            try { context.Response.Close(); } catch { /* уже закрыт */ }
        }
    }
}
