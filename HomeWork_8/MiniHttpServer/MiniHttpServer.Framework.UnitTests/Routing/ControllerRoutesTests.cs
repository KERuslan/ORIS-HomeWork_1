using Microsoft.VisualStudio.TestTools.UnitTesting;
using MyHttpServer.Framework.Attributes;
using MyHttpServer.Framework.Routing;

namespace MiniHttpServer.Framework.UnitTests;

[TestClass]
public sealed class ControllerRoutesTests
{
    private readonly ControllerRoutes _routes = new(typeof(DemoController), typeof(UnmarkedController));

    [TestMethod]
    public void Find_UsesControllerAndMethodAttributes()
    {
        Assert.AreEqual(nameof(DemoController.Details), _routes.Find("GET", "/demo/details/")?.Action.Name);
        Assert.IsNotNull(_routes.Find("POST", "/demo/submit"));
        Assert.IsNull(_routes.Find("GET", "/demo/submit"));
        Assert.IsNull(_routes.Find("GET", "/demo/unmarked"));
        Assert.IsNull(_routes.Find("GET", "/unmarked/details"));
    }

    [TestMethod]
    public void TryBind_ConvertsQueryByName()
    {
        var parameters = _routes.Find("GET", "/demo/details")!.Action.GetParameters();
        var query = RequestParameters.ParseForm("name=Alex&id=15");

        Assert.IsTrue(RequestParameters.TryBind(parameters, query, out var arguments));
        CollectionAssert.AreEqual(new object[] { 15, "Alex" }, arguments);
    }

    [TestMethod]
    public void TryBind_DecodesFormAndConvertsBody()
    {
        var parameters = _routes.Find("POST", "/demo/submit")!.Action.GetParameters();
        var form = RequestParameters.ParseForm("message=one+two%2Bthree&count=2");

        Assert.IsTrue(RequestParameters.TryBind(parameters, form, out var arguments));
        CollectionAssert.AreEqual(new object[] { "one two+three", 2 }, arguments);
    }

    [DataTestMethod]
    [DataRow("id=bad&name=Alex")]
    [DataRow("id=2")]
    [DataRow("id=1&id=2&name=Alex")]
    [DataRow("id=999999999999999999&name=Alex")]
    public void TryBind_RejectsInvalidArguments(string text)
    {
        var parameters = _routes.Find("GET", "/demo/details")!.Action.GetParameters();
        var query = RequestParameters.ParseForm(text);

        Assert.IsFalse(RequestParameters.TryBind(parameters, query, out _));
    }

    [Controller]
    public sealed class DemoController
    {
        [Get] public string Details(int id, string name) => name;
        [Post] public string Submit(string message, int count) => message;
        public string Unmarked() => "";
    }

    public sealed class UnmarkedController
    {
        [Get] public string Details() => "";
    }
}
