namespace MyHttpServer.Cli;

public interface IConsoleCommand
{
    string Name { get; }
    void Execute();
}
