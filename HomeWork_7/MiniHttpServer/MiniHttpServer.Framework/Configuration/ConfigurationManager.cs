using System.Text.Json;

namespace MyHttpServer.Framework.Configuration;

public static class ConfigurationManager
{
    private static readonly Lazy<ServerSettings> Settings = new(() =>
    {
        string path = Path.Combine(AppContext.BaseDirectory, "settings.json");
        return JsonSerializer.Deserialize<ServerSettings>(File.ReadAllText(path))
            ?? throw new InvalidDataException("Не удалось прочитать settings.json.");
    });

    public static ServerSettings Current => Settings.Value;
}
