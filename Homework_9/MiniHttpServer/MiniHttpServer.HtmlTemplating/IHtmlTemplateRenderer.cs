namespace MiniHttpServer.HtmlTemplating;

public interface IHtmlTemplateRenderer
{
    string Render(string template, object model);
}
