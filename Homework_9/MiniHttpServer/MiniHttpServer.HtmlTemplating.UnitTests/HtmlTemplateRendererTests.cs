using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MiniHttpServer.HtmlTemplating.UnitTests;

[TestClass]
public sealed class HtmlTemplateRendererTests
{
    private readonly HtmlTemplateRenderer _renderer = new();

    [TestMethod]
    public void Render_EncodesValuesAndRepeatsRows()
    {
        var model = new { Users = new[] { new { Name = "A<B" }, new { Name = "C&D" } } };
        const string template = "<ul>$foreach(var user in Users)<li>${user.Name}</li>$endfor</ul>";

        Assert.AreEqual("<ul><li>A&lt;B</li><li>C&amp;D</li></ul>", _renderer.Render(template, model));
    }

    [TestMethod]
    public void Render_EmptyCollectionLeavesOnlySurroundingMarkup()
    {
        var model = new { Users = Array.Empty<string>() };
        Assert.AreEqual("[]", _renderer.Render("[$foreach(var user in Users)${user}$endfor]", model));
    }

    [TestMethod]
    public void Render_NestedLoopRestoresOuterVariable()
    {
        var model = new { Groups = new[] { new { Name = "Group", Items = new[] { "One", "Two" } } } };
        const string template = "$foreach(var item in Groups)$foreach(var item in item.Items)${item},$endfor${item.Name}$endfor";
        Assert.AreEqual("One,Two,Group", _renderer.Render(template, model));
    }

    [TestMethod]
    public void Render_UnclosedLoopThrows()
    {
        Assert.ThrowsException<FormatException>(() => _renderer.Render("$foreach(var user in Users)", new { }));
    }
}
