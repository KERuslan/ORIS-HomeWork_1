using System.Text.Json;
using MyHttpServer;

if (!File.Exists("settings.json"))
{
    Console.WriteLine("Файл settings.json не найден");
    return;
}

string settingsJson = File.ReadAllText("settings.json");
Settings? settings = JsonSerializer.Deserialize<Settings>(settingsJson);

if (settings is null)
{
    Console.WriteLine("Не удалось прочитать настройки из settings.json");
    return;
}

var httpServer = new HttpServer(settings);
httpServer.Start();

Console.WriteLine("Сервер запущен. Введите 'stop' для остановки");

while (true)
{
    string? command = Console.ReadLine();
    if (command == "stop")
    {
        httpServer.Stop();
        Console.WriteLine("Сервер остановлен");
        break;
    }
}