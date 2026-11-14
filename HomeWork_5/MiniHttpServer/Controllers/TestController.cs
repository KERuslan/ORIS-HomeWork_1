using System.Net;
using MyHttpServer.Framework.Attributes;

namespace MyHttpServer.Controllers;

[Controller]
public sealed class TestController
{
    [Get]
    public string Index() => "<h1>Hello World!</h1>";

    [Get]
    public string Hello() => "<h1>Привет!</h1>";

    [Get]
    public string Details(int id, string name) => $"ID: {id}, Name: {WebUtility.HtmlEncode(name)}";

    [Post]
    public string Submit(string demoUser, string message) =>
        $"Тестовое имя: {WebUtility.HtmlEncode(demoUser)}. Сообщение: {WebUtility.HtmlEncode(message)}";
}
