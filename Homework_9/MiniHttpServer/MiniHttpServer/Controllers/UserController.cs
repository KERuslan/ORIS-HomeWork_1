using MiniHttpServer.Framework.Results;
using MiniHttpServer.HtmlTemplating;
using MiniHttpServer.Models;
using MiniHttpServer.ORM;

namespace MiniHttpServer.Controllers;

public sealed class UserController(IOrmContext orm, IHtmlTemplateRenderer renderer, string templateRoot)
{
    public async Task<IHttpResult> List()
    {
        var users = await orm.GetAllAsync<User>("Users");
        return new PageResult(Path.Combine(templateRoot, "User", "List.html"), new { Users = users }, renderer);
    }
}
