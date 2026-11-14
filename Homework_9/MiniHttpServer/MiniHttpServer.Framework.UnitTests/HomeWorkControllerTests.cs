using System.Net;
using System.Net.Mail;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MyHttpServer.Controllers;
using MyHttpServer.Core.ContentTypes;
using MyHttpServer.Core.Handlers;
using HttpServa.Tests.TestSupport;

namespace HttpServa.Tests;

[TestClass]
public class HomeWorkControllerTests
{
    [TestMethod]
    public async Task Send_WithoutEmail_ReturnsBadRequest()
    {
        var writer = new ResponseWriter(new ContentTypeResolver());
        var sender = new FakeEmailSender();
        var controller = new HomeWorkController(writer, sender, () => new MemoryStream());

        var result = await HttpTestHost.RunAsync("/home-work/send", "GET", controller.Send);

        Assert.AreEqual(400, result.StatusCode);
        Assert.IsTrue(result.Body.Contains("Не указан параметр to"));
        Assert.IsFalse(sender.WasCalled);
    }

    [TestMethod]
    public async Task Send_WithValidEmail_SendsMessageAndReturnsSuccess()
    {
        var writer = new ResponseWriter(new ContentTypeResolver());
        var sender = new FakeEmailSender();
        var controller = new HomeWorkController(writer, sender, () => new MemoryStream(new byte[] { 1, 2, 3 }));

        var result = await HttpTestHost.RunAsync("/home-work/send?to=user%40example.com", "GET", controller.Send);

        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(result.Body.Contains("Письмо отправлено"));
        Assert.IsTrue(sender.WasCalled);
        Assert.AreEqual("user@example.com", sender.Recipient);
        Assert.IsTrue(sender.Body.Contains("Группа: 11-541"));
        Assert.AreEqual(1, sender.AttachmentCount);
    }

    [TestMethod]
    public async Task Send_WithInvalidEmail_ReturnsServerError()
    {
        var writer = new ResponseWriter(new ContentTypeResolver());
        var sender = new FakeEmailSender();
        var controller = new HomeWorkController(writer, sender, () => new MemoryStream());

        var result = await HttpTestHost.RunAsync("/home-work/send?to=wrong-address", "GET", controller.Send);

        Assert.AreEqual(500, result.StatusCode);
        Assert.IsTrue(result.Body.Contains("Не удалось отправить письмо"));
        Assert.IsFalse(sender.WasCalled);
    }


    [TestMethod]
    public async Task Send_WhenEmailServiceFails_ReturnsServerError()
    {
        var writer = new ResponseWriter(new ContentTypeResolver());
        var sender = new FailingEmailSender();
        var controller = new HomeWorkController(writer, sender, () => new MemoryStream());

        var result = await HttpTestHost.RunAsync("/home-work/send?to=user%40example.com", "GET", controller.Send);

        Assert.AreEqual(500, result.StatusCode);
        Assert.IsTrue(result.Body.Contains("Не удалось отправить письмо"));
    }

    private sealed class FailingEmailSender : IEmailSender
    {
        public Task SendAsync(MailMessage message)
        {
            throw new InvalidOperationException("SMTP settings are missing.");
        }
    }

    private sealed class FakeEmailSender : IEmailSender
    {
        public bool WasCalled { get; private set; }
        public string Recipient { get; private set; } = "";
        public string Body { get; private set; } = "";
        public int AttachmentCount { get; private set; }

        public Task SendAsync(MailMessage message)
        {
            WasCalled = true;
            Recipient = message.To[0].Address;
            Body = message.Body;
            AttachmentCount = message.Attachments.Count;
            return Task.CompletedTask;
        }
    }
}
