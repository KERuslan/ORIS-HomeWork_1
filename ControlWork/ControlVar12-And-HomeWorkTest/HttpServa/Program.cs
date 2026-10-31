using MyHttpServer.Cli;
using MyHttpServer.Controllers;
using MyHttpServer.Core;
using MyHttpServer.Core.ContentTypes;
using MyHttpServer.Core.Handlers;

const string BaseUrl = "http://127.0.0.1:8080/";
const string IndexFile = "index.html";

string staticRoot = Path.Combine(AppContext.BaseDirectory, "static");

var writer = new ResponseWriter(new ContentTypeResolver());

var pipeline = new RequestPipeline(new IRequestHandler[]
{
    new HomeWorkHandler(new HomeWorkController(writer)),
    new ReviewsHandler(new ReviewsController(writer)),
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
dispatcher.Register(new OpenSiteCommand("reviews", BaseUrl + "reviews"));
dispatcher.Register(new OpenSiteCommand("send", BaseUrl + "home-work/send?to=test@ttt.ru"));
dispatcher.Register(new StopCommand(() => { server.Stop(); running = false; }));

Console.WriteLine("Команды: reviews, send, stop");

while (running)
{
    string? line = Console.ReadLine();
    if (line is null) { server.Stop(); break; }
    dispatcher.Dispatch(line);
}
