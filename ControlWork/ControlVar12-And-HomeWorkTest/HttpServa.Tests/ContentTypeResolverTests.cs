using Microsoft.VisualStudio.TestTools.UnitTesting;
using MyHttpServer.Core.ContentTypes;

namespace HttpServa.Tests;

[TestClass]
public class ContentTypeResolverTests
{
    [TestMethod]
    public void Resolve_ReturnsHtmlWithUtf8Charset()
    {
        var resolver = new ContentTypeResolver();
        Assert.AreEqual("text/html; charset=utf-8", resolver.Resolve("page.html"));
    }

    [TestMethod]
    public void Resolve_ReturnsImageTypeWithoutCharset()
    {
        var resolver = new ContentTypeResolver();
        Assert.AreEqual("image/png", resolver.Resolve("image.png"));
    }

    [TestMethod]
    public void Resolve_ReturnsFallbackForUnknownExtension()
    {
        var resolver = new ContentTypeResolver();
        Assert.AreEqual(MimeTypeMap.Default, resolver.Resolve("file.unknown"));
    }

    [TestMethod]
    public void Resolve_UsesCustomMap()
    {
        var resolver = new ContentTypeResolver(new Dictionary<string, string> { [".custom"] = "text/plain" });
        Assert.AreEqual("text/plain; charset=utf-8", resolver.Resolve("file.custom"));
    }
}
