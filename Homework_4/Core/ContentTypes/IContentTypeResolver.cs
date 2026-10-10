namespace MyHttpServer.Core.ContentTypes;

public interface IContentTypeResolver
{
    string Resolve(string filePath);
}
