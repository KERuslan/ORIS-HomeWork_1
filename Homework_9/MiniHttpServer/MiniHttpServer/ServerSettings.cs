namespace MiniHttpServer;

internal sealed class ServerSettings
{
    public int Port { get; init; } = 8080;
    public string PublicDirectoryPath { get; init; } = "static";
    public required string ConnectionString { get; init; }
}
