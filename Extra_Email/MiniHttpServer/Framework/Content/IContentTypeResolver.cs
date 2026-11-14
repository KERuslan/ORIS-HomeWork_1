namespace MyHttpServer.Framework.Content;

public interface IContentTypeResolver
{
    string Resolve(string filePath);
}
