namespace MyHttpServer.Cli;

public sealed class CommandDispatcher
{
    private readonly Dictionary<string, IConsoleCommand> _commands = new(StringComparer.OrdinalIgnoreCase);

    public void Register(IConsoleCommand command) => _commands[command.Name] = command;

    public void Dispatch(string? line)
    {
        string name = line?.Trim() ?? "";
        if (name.Length == 0) return;

        if (_commands.TryGetValue(name, out var command))
            command.Execute();
        else
            Console.WriteLine($"Неизвестная команда '{name}'. Доступно: {string.Join(", ", _commands.Keys)}");
    }
}
