using System.Net;
using System.Net.Sockets;

namespace HttpServa.Tests.TestSupport;

internal static class HttpTestHost
{
    public static async Task<(int StatusCode, string Body)> RunAsync(
        string path,
        string method,
        Func<HttpListenerContext, Task> action,
        string? requestBody = null,
        string contentType = "application/x-www-form-urlencoded")
    {
        // Find a free local port for the test server.
        var tcp = new TcpListener(IPAddress.Loopback, 0);
        tcp.Start();
        int port = ((IPEndPoint)tcp.LocalEndpoint).Port;
        tcp.Stop();

        // Start an HTTP listener on the free port.
        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        listener.Start();

        // Wait for one request and run the test action.
        Task serverTask = Task.Run(async () =>
        {
            var context = await listener.GetContextAsync();
            try
            {
                await action(context);
            }
            finally
            {
                context.Response.Close();
            }
        });

        // Send the request to the local listener.
        using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
        using var request = new HttpRequestMessage(new HttpMethod(method), $"http://127.0.0.1:{port}{path}");
        if (requestBody is not null) request.Content = new StringContent(requestBody, System.Text.Encoding.UTF8, contentType);
        using var response = await client.SendAsync(request);
        string body = await response.Content.ReadAsStringAsync();
        await serverTask;
        return ((int)response.StatusCode, body);
    }
}
