using MyHttpServer;

var settings = ConfigurationManager.Current;
var httpServer = new HttpServer(settings);
httpServer.Start();

Console.WriteLine("Сервер запущен. Введите 'stop' для остановки");

while (true)
{
    string? command = Console.ReadLine();
    if (command is null || command == "stop")
    {
        httpServer.Stop();
        Console.WriteLine("Сервер остановлен");
        break;
    }
}
