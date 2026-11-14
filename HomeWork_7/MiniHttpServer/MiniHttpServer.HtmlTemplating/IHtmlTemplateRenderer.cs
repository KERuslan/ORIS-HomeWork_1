namespace MyHttpServer.HtmlTemplating;

public interface IHtmlTemplateRenderer
{
    string RenderFromString(string htmlTemplate, object dataModel);
    string RenderFromFile(string filePath, object dataModel);
    string RenderToFile(string templatePath, string outputPath, object dataModel);
}
