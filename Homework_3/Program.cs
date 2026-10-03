using MyHttpServer.Cli;
using MyHttpServer.Core;
using MyHttpServer.Core.ContentTypes;
using MyHttpServer.Core.Handlers;

const string BaseUrl = "http://127.0.0.1:8080/";
const string MainSite = "search";

string staticRoot = Path.Combine(AppContext.BaseDirectory, "static");
string mainPage = Path.Combine(staticRoot, MainSite, MainSite + ".html");

if (!File.Exists(mainPage))
{
    Console.WriteLine($"Ошибка: не найдена главная страница {mainPage}");
    return;
}

var writer = new ResponseWriter(new ContentTypeResolver());

var pipeline = new RequestPipeline(new IRequestHandler[]
{
    new MethodGuardHandler(writer),
    new StaticFileHandler(staticRoot, MainSite, writer),
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
dispatcher.Register(new OpenSiteCommand("steam", BaseUrl + "steam/"));
dispatcher.Register(new OpenSiteCommand("satisfactory", BaseUrl + "Satisfactory/"));
dispatcher.Register(new StopCommand(() => { server.Stop(); running = false; }));

Console.WriteLine("Команды: search, steam, satisfactory, stop");

while (running)
{
    string? line = Console.ReadLine();
    if (line is null) { server.Stop(); break; } 
    dispatcher.Dispatch(line);
}
