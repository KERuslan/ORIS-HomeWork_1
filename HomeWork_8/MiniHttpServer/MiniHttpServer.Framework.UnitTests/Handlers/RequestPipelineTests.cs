using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MyHttpServer.Framework.Handlers;

namespace MiniHttpServer.Framework.UnitTests;

[TestClass]
public sealed class RequestPipelineTests
{
    [TestMethod]
    public async Task ProcessAsync_ContinuesUntilRequestIsHandled()
    {
        var first = new StubHandler(false);
        var second = new StubHandler(true);
        var third = new StubHandler(true);
        var pipeline = new RequestPipeline([first, second, third]);

        await pipeline.ProcessAsync(null!);

        Assert.IsTrue(first.Called);
        Assert.IsTrue(second.Called);
        Assert.IsFalse(third.Called);
    }

    private sealed class StubHandler(bool handled) : IRequestHandler
    {
        public bool Called { get; private set; }

        public Task<bool> HandleAsync(HttpListenerContext context)
        {
            Called = true;
            return Task.FromResult(handled);
        }
    }
}
