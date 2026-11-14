using MyHttpServer.Framework.Attributes;
using MyHttpServer.Framework.Controllers;
using MyHttpServer.Framework.Results;

namespace MyHttpServer.Controllers;

[Controller]
public sealed class TestController : BaseController
{
    [Get]
    public IHttpResult Index() => Html("<h1>Hello World!</h1>");

    [Get]
    public IHttpResult Hello() => Html("<h1>Привет!</h1>");

    [Get]
    public IHttpResult Details(int id, string name) => Json(new { id, name });

    [Post]
    public IHttpResult Submit(string demoUser, string message) => Json(new { demoUser, message });

    [Get]
    public IHttpResult Info() => Json(new { Name = "Student", Group = "11-541", Course = "ORIS" });

    [Get]
    public IHttpResult RequestInfo() => Json(Context);
}
