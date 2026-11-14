using Microsoft.VisualStudio.TestTools.UnitTesting;
using MyHttpServer.Framework.Results;
using MyHttpServer.HtmlTemplating;

namespace MiniHttpServer.Framework.UnitTests;

[TestClass]
public sealed class ResultTests
{
    [TestMethod]
    public void HtmlResult_PreservesBodyAndStatus()
    {
        var result = new HtmlResult("<p>Привет</p>", 201);

        Assert.AreEqual(201, result.StatusCode);
        Assert.AreEqual("text/html; charset=utf-8", result.ContentType);
        Assert.AreEqual("<p>Привет</p>", result.Render());
    }

    [TestMethod]
    public void JsonResult_SerializesCamelCase()
    {
        var result = new JsonResult(new { IsActive = true }, 400);

        Assert.AreEqual(400, result.StatusCode);
        Assert.AreEqual("application/json; charset=utf-8", result.ContentType);
        Assert.AreEqual("{\"isActive\":true}", result.Render());
    }

    [TestMethod]
    public void PageResult_PassesTemplateAndModelToRenderer()
    {
        var renderer = new FakeRenderer();
        object model = new { Name = "Alex" };
        var result = new PageResult(renderer, "profile.html", model);

        Assert.AreEqual("<p>Профиль</p>", result.Render());
        Assert.AreEqual("profile.html", renderer.Path);
        Assert.AreSame(model, renderer.Model);
    }

    private sealed class FakeRenderer : IHtmlTemplateRenderer
    {
        public string? Path { get; private set; }
        public object? Model { get; private set; }

        public string RenderFromFile(string filePath, object dataModel)
        {
            Path = filePath;
            Model = dataModel;
            return "<p>Профиль</p>";
        }

        public string RenderFromString(string htmlTemplate, object dataModel) => throw new NotSupportedException();
        public string RenderToFile(string templatePath, string outputPath, object dataModel) => throw new NotSupportedException();
    }
}
