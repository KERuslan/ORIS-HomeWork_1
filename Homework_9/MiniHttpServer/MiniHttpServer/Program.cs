using System.Text.Json;
using MiniHttpServer;
using MiniHttpServer.Controllers;
using MiniHttpServer.HtmlTemplating;
using MiniHttpServer.ORM;
using MyHttpServer.Cli;
using MyHttpServer.Controllers;
using MyHttpServer.Core;
using MyHttpServer.Core.ContentTypes;
using MyHttpServer.Core.Handlers;

const string IndexFile = "index.html";

string baseDirectory = AppContext.BaseDirectory;
var settings = JsonSerializer.Deserialize<ServerSettings>(
    File.ReadAllText(Path.Combine(baseDirectory, "settings.json")))
    ?? throw new InvalidOperationException("Не удалось загрузить настройки сервера.");

string baseUrl = $"http://127.0.0.1:{settings.Port}/";
string staticRoot = Path.Combine(baseDirectory, settings.PublicDirectoryPath);
string connectionString = Environment.GetEnvironmentVariable("ORIS_CONNECTION_STRING")
    ?? settings.ConnectionString;

var writer = new ResponseWriter(new ContentTypeResolver());
var userController = new UserController(new OrmContext(connectionString), new HtmlTemplateRenderer(),
    Path.Combine(baseDirectory, "Templates"));
var controllers = new ControllersHandler(writer);
controllers.MapGet("/user/list", userController.List);

var pipeline = new RequestPipeline(new IRequestHandler[]
{
    new HomeWorkHandler(new HomeWorkController(writer)),
    controllers,
    new MethodGuardHandler(writer),
    new StaticFileHandler(staticRoot, IndexFile, writer),
    new NotFoundHandler(Path.Combine(staticRoot, "404.html"), writer)
});

var server = new HttpServer(baseUrl, pipeline);

try
{
    server.Start();
}
catch (System.Net.HttpListenerException e)
{
    Console.WriteLine($"Не удалось запустить сервер: {e.Message} (возможно, порт занят)");
    return;
}

bool running = true;
var dispatcher = new CommandDispatcher();
dispatcher.Register(new OpenSiteCommand("users", baseUrl + "user/list"));
dispatcher.Register(new OpenSiteCommand("send", baseUrl + "home-work/send?to=test@ttt.ru"));
dispatcher.Register(new StopCommand(() => { server.Stop(); running = false; }));

Console.WriteLine("Команды: users, send, stop");

while (running)
{
    string? line = Console.ReadLine();
    if (line is null) { server.Stop(); break; }
    dispatcher.Dispatch(line);
}
