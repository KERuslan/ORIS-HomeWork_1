using Microsoft.VisualStudio.TestTools.UnitTesting;
using MyHttpServer.HtmlTemplating;

namespace MiniHttpServer.HtmlTemplating.UnitTests;

[TestClass]
public sealed class HtmlTemplateRendererTests
{
    private readonly HtmlTemplateRenderer _renderer = new();

    [DataTestMethod]
    [DataRow("${Name}", "Alex")]
    [DataRow("${Name} ${Age} ${Name}", "Alex 18 Alex")]
    [DataRow("${User.Name}", "Sam")]
    [DataRow("", "")]
    [DataRow("${Markup}", "&lt;b&gt;")]
    public void RenderFromString_SubstitutesProperties(string template, string expected)
    {
        var model = new { Name = "Alex", Age = 18, User = new { Name = "Sam" }, Markup = "<b>" };

        Assert.AreEqual(expected, _renderer.RenderFromString(template, model));
    }

    [DataTestMethod]
    [DataRow("$if(Active)да$endif", true, "да")]
    [DataRow("$if(Active)да$endif", false, "")]
    [DataRow("$if(Active)да$else нет$endif", false, " нет")]
    [DataRow("$if(Active)$if(Admin)да$else нет$endif$endif", true, " нет")]
    public void RenderFromString_SelectsConditionBranch(string template, bool active, string expected)
    {
        Assert.AreEqual(expected, _renderer.RenderFromString(template, new { Active = active, Admin = false }));
    }

    [TestMethod]
    public void RenderFromString_IteratesNestedCollectionsAndChecksConditions()
    {
        const string template = "$foreach(var group in Groups)$foreach(var user in group.Users)$if(user.Active)${user.Name},$endif$endfor$endfor";
        var groups = new[]
        {
            new { Users = new[] { new { Name = "Alex", Active = true }, new { Name = "Sam", Active = false } } },
            new { Users = new[] { new { Name = "Ann", Active = true } } }
        };

        Assert.AreEqual("Alex,Ann,", _renderer.RenderFromString(template, new { Groups = groups }));
    }

    [DataTestMethod]
    [DataRow(0, "")]
    [DataRow(2, "12")]
    public void RenderFromString_HandlesEmptyAndPopulatedCollections(int count, string expected)
    {
        const string template = "$foreach(var item in Items)${item}$endfor";

        Assert.AreEqual(expected, _renderer.RenderFromString(template, new { Items = Enumerable.Range(1, count) }));
    }

    [DataTestMethod]
    [DataRow("$if(Active)")]
    [DataRow("$foreach(var item in Items)")]
    [DataRow("$foreach(var item Items)$endfor")]
    [DataRow("$endif")]
    [DataRow("${Missing}")]
    public void RenderFromString_RejectsInvalidTemplate(string template)
    {
        Assert.ThrowsException<FormatException>(() => _renderer.RenderFromString(template, new { Active = true }));
    }

    [TestMethod]
    public void RenderFile_ReadsTemplateAndWritesResult()
    {
        var dir = Directory.CreateTempSubdirectory();
        string template = Path.Combine(dir.FullName, "template.html");
        string output = Path.Combine(dir.FullName, "output.html");
        try
        {
            File.WriteAllText(template, "<b>${Name}</b>");
            var model = new { Name = "Alex" };

            Assert.AreEqual("<b>Alex</b>", _renderer.RenderFromFile(template, model));
            Assert.AreEqual("<b>Alex</b>", _renderer.RenderToFile(template, output, model));
            Assert.AreEqual("<b>Alex</b>", File.ReadAllText(output));
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }
}
