using MyHttpServer.Controllers;
using MyHttpServer.Framework.Routing;
using MyHttpServer.Cli;
using MyHttpServer.Framework;
using MyHttpServer.Framework.Content;
using MyHttpServer.Framework.Handlers;

var config = MyHttpServer.Framework.Configuration.ConfigurationManager.Current.Server;
string BaseUrl = $"http://{config.Host}:{config.Port}/";
const string MainSite = "SearcherFinder";

string staticRoot = Path.Combine(AppContext.BaseDirectory, "static");
string mainPage = Path.Combine(staticRoot, MainSite, "index.html");

if (!File.Exists(mainPage))
{
    Console.WriteLine($"Ошибка: не найдена главная страница {mainPage}");
    return;
}

var writer = new ResponseWriter(new ContentTypeResolver());

var pipeline = new RequestPipeline(new IRequestHandler[]
{
    new ControllersHandler(new ControllerRoutes(typeof(TestController), typeof(UserController)), writer),
    new MethodGuardHandler(writer),
    new StaticFilesHandler(staticRoot, MainSite, writer),
    new NotFoundHandler(Path.Combine(staticRoot, "404.html"), writer)
});

var server = new HttpServer(BaseUrl, pipeline);

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
dispatcher.Register(new OpenSiteCommand("search", BaseUrl));
dispatcher.Register(new OpenSiteCommand("steam", BaseUrl + "Steam/"));
dispatcher.Register(new OpenSiteCommand("satisfactory", BaseUrl + "Satisfactory/"));
dispatcher.Register(new StopCommand(() => { server.Stop(); running = false; }));

Console.WriteLine("Команды: search, steam, satisfactory, stop");

while (running)
{
    string? line = Console.ReadLine();
    if (line is null) { server.Stop(); break; } 
    dispatcher.Dispatch(line);
}
