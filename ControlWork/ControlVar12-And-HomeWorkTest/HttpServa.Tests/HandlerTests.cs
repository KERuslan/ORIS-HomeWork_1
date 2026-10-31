using System.Net;
using System.Net.Mail;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MyHttpServer.Controllers;
using MyHttpServer.Core.ContentTypes;
using MyHttpServer.Core.Handlers;
using HttpServa.Tests.TestSupport;

namespace HttpServa.Tests;

[TestClass]
public class HandlerTests
{
    [TestMethod]
    public async Task HomeWorkHandler_HandlesEmailRoute()
    {
        var writer = new ResponseWriter(new ContentTypeResolver());
        var sender = new RecordingEmailSender();
        var controller = new HomeWorkController(writer, sender, () => new MemoryStream());
        var handler = new HomeWorkHandler(controller);

        var result = await HttpTestHost.RunAsync("/home-work/send?to=test%40example.com", "GET", async context =>
        {
            bool handled = await handler.HandleAsync(context);
            Assert.IsTrue(handled);
        });

        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(sender.Called);
    }

    [TestMethod]
    public async Task HomeWorkHandler_ReturnsFalseForOtherRoute()
    {
        var writer = new ResponseWriter(new ContentTypeResolver());
        var controller = new HomeWorkController(writer, new RecordingEmailSender(), () => new MemoryStream());
        var handler = new HomeWorkHandler(controller);

        var result = await HttpTestHost.RunAsync("/other", "GET", async context =>
        {
            bool handled = await handler.HandleAsync(context);
            Assert.IsFalse(handled);
        });

        Assert.AreEqual(200, result.StatusCode);
    }

    [TestMethod]
    public async Task HomeWorkHandler_ReturnsFalseForPostRequest()
    {
        var writer = new ResponseWriter(new ContentTypeResolver());
        var controller = new HomeWorkController(writer, new RecordingEmailSender(), () => new MemoryStream());
        var handler = new HomeWorkHandler(controller);

        await HttpTestHost.RunAsync("/home-work/send", "POST", async context =>
        {
            bool handled = await handler.HandleAsync(context);
            Assert.IsFalse(handled);
        });
    }

    [TestMethod]
    public async Task MethodGuardHandler_RejectsPostRequest()
    {
        var writer = new ResponseWriter(new ContentTypeResolver());
        var handler = new MethodGuardHandler(writer);

        var result = await HttpTestHost.RunAsync("/", "POST", async context =>
        {
            bool handled = await handler.HandleAsync(context);
            Assert.IsTrue(handled);
        });

        Assert.AreEqual(405, result.StatusCode);
    }

    [TestMethod]
    public async Task MethodGuardHandler_AllowsGetRequest()
    {
        var handler = new MethodGuardHandler(new ResponseWriter(new ContentTypeResolver()));

        var result = await HttpTestHost.RunAsync("/", "GET", async context =>
        {
            bool handled = await handler.HandleAsync(context);
            Assert.IsFalse(handled);
        });

        Assert.AreEqual(200, result.StatusCode);
    }

    [TestMethod]
    public async Task StaticFileHandler_SendsExistingFile()
    {
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(Path.Combine(root, "hello.txt"), "Hello");
        try
        {
            var handler = new StaticFileHandler(root, "index.html", new ResponseWriter(new ContentTypeResolver()));
            var result = await HttpTestHost.RunAsync("/hello.txt", "GET", async context =>
            {
                bool handled = await handler.HandleAsync(context);
                Assert.IsTrue(handled);
            });

            Assert.AreEqual(200, result.StatusCode);
            Assert.AreEqual("Hello", result.Body);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [TestMethod]
    public async Task NotFoundHandler_Returns404WhenFileDoesNotExist()
    {
        string missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "404.html");
        var handler = new NotFoundHandler(missing, new ResponseWriter(new ContentTypeResolver()));

        var result = await HttpTestHost.RunAsync("/missing", "GET", async context =>
        {
            bool handled = await handler.HandleAsync(context);
            Assert.IsTrue(handled);
        });

        Assert.AreEqual(404, result.StatusCode);
        Assert.AreEqual("404 Not Found", result.Body);
    }


    [TestMethod]
    public async Task RequestPipeline_StopsAfterFirstHandlerReturnsTrue()
    {
        var first = new StubHandler(true);
        var second = new StubHandler(false);
        var pipeline = new RequestPipeline(new IRequestHandler[] { first, second });

        await HttpTestHost.RunAsync("/", "GET", async context => await pipeline.ProcessAsync(context));

        Assert.AreEqual(1, first.CallCount);
        Assert.AreEqual(0, second.CallCount);
    }

    [TestMethod]
    public async Task RequestPipeline_CallsNextHandlerWhenCurrentHandlerReturnsFalse()
    {
        var first = new StubHandler(false);
        var second = new StubHandler(true);
        var pipeline = new RequestPipeline(new IRequestHandler[] { first, second });

        await HttpTestHost.RunAsync("/", "GET", async context => await pipeline.ProcessAsync(context));

        Assert.AreEqual(1, first.CallCount);
        Assert.AreEqual(1, second.CallCount);
    }

    private sealed class StubHandler : IRequestHandler
    {
        private readonly bool _result;

        public StubHandler(bool result)
        {
            _result = result;
        }

        public int CallCount { get; private set; }

        public Task<bool> HandleAsync(HttpListenerContext context)
        {
            CallCount++;
            return Task.FromResult(_result);
        }
    }

    private sealed class RecordingEmailSender : IEmailSender
    {
        public bool Called { get; private set; }

        public Task SendAsync(MailMessage message)
        {
            Called = true;
            return Task.CompletedTask;
        }
    }
}
