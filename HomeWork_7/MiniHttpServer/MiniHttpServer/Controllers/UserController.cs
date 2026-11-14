using MyHttpServer.Framework.Attributes;
using MyHttpServer.Framework.Controllers;
using MyHttpServer.Framework.Results;
using MyHttpServer.HtmlTemplating;

namespace MyHttpServer.Controllers;

[Controller]
public sealed class UserController : BaseController
{
    private readonly HtmlTemplateRenderer _renderer = new();

    [Get]
    public IHttpResult Profile() => Page("Profile.html", new { Name = "Алекс", Group = "11-541", IsActive = true });

    [Get]
    public IHttpResult List() => Page("List.html", new
    {
        Users = new[] { new { Name = "Алекс", IsActive = true }, new { Name = "Мария", IsActive = false } }
    });

    private IHttpResult Page(string file, object model) =>
        new PageResult(_renderer, Path.Combine(AppContext.BaseDirectory, "Templates", "User", file), model);
}
