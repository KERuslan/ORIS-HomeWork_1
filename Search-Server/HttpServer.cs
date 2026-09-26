using System.Net;
using System.Text;

namespace MyHttpServer;

public class HttpServer
{
    private readonly HttpListener _listener = new();
    private readonly string _uriPrefix;

    public HttpServer(Settings settings)
    {
        _uriPrefix = $"http://{settings.Server.Host}:{settings.Server.Port}/{settings.Server.Path}";
    }

    public void Start()
    {
        _listener.Prefixes.Add(_uriPrefix);
        _listener.Start();
        _ = ListenLoopAsync();
    }

    public void Stop()
    {
        _listener.Stop();
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
            catch (HttpListenerException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }

            _ = HandleRequestAsync(context);
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        HttpListenerResponse response = context.Response;

        if (!File.Exists("Search.html"))
        {
            Console.WriteLine("Файл Search.html не найден");
            response.StatusCode = 404;
            response.Close();
            return;
        }

        string htmlFileText = await File.ReadAllTextAsync("Search.html");
        byte[] buffer = Encoding.UTF8.GetBytes(htmlFileText);
        response.ContentLength64 = buffer.Length;

        using Stream output = response.OutputStream;
        await output.WriteAsync(buffer);
        await output.FlushAsync();

        Console.WriteLine("Запрос обработан");
    }
}