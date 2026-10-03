namespace MyHttpServer;

public class Settings
{
    public Server Server { get; set; } = new();
}

public class Server
{
    public string Port { get; set; } = "8080";
    public string Host { get; set; } = "127.0.0.1";
    public string Path { get; set; } = "search";
}