using System.Diagnostics;

namespace MyHttpServer.Cli;

public sealed class OpenSiteCommand : IConsoleCommand
{
    private readonly string _url;

    public OpenSiteCommand(string name, string url)
    {
        Name = name;
        _url = url;
    }

    public string Name { get; }

    public void Execute()
    {
        Console.WriteLine($"Переход: {_url}");
        try
        {
            Process.Start(new ProcessStartInfo(_url) { UseShellExecute = true });
        }
        catch (Exception)
        {
            Console.WriteLine("Не удалось открыть браузер — откройте ссылку вручную");
        }
    }
}

public sealed class StopCommand : IConsoleCommand
{
    private readonly Action _stop;

    public StopCommand(Action stop) => _stop = stop;

    public string Name => "stop";

    public void Execute() => _stop();
}
