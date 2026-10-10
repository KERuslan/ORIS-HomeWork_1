using MyHttpServer.Cli;
using MyHttpServer.Core;
using MyHttpServer.Core.ContentTypes;
using MyHttpServer.Core.Handlers;

const string BaseUrl = "http://127.0.0.1:8080/";
const string IndexFile = "login.html";

string staticRoot = Path.Combine(AppContext.BaseDirectory, "static");
string loginPage = Path.Combine(staticRoot, IndexFile);

if (!File.Exists(loginPage))
{
    Console.WriteLine($"Ошибка: не найдена главная страница {loginPage}");
    return;
}

var writer = new ResponseWriter(new ContentTypeResolver());

var pipeline = new RequestPipeline(new IRequestHandler[]
{
    new LoginHandler(loginPage, writer),
    new MethodGuardHandler(writer),
    new StaticFileHandler(staticRoot, IndexFile, writer),
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
dispatcher.Register(new OpenSiteCommand("login", BaseUrl + "login"));
dispatcher.Register(new StopCommand(() => { server.Stop(); running = false; }));

Console.WriteLine("Команды: login, stop");

while (running)
{
    string? line = Console.ReadLine();
    if (line is null) { server.Stop(); break; } 
    dispatcher.Dispatch(line);
}
