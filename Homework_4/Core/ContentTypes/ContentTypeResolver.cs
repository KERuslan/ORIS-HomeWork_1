namespace MyHttpServer.Core.ContentTypes;

public sealed class ContentTypeResolver : IContentTypeResolver
{
    private static readonly HashSet<string> TextLike = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/json", "application/ld+json", "application/manifest+json",
        "application/xml", "application/xhtml+xml", "image/svg+xml"
    };

    private readonly IReadOnlyDictionary<string, string> _map;
    private readonly string _fallback;

    public ContentTypeResolver(IReadOnlyDictionary<string, string>? map = null,
                               string fallback = MimeTypeMap.Default)
    {
        _map = map ?? MimeTypeMap.Map;
        _fallback = fallback;
    }

    public string Resolve(string filePath)
    {
        string extension = Path.GetExtension(filePath);

        if (string.IsNullOrEmpty(extension) || !_map.TryGetValue(extension, out var type))
            return _fallback;

        return NeedsCharset(type) ? $"{type}; charset=utf-8" : type;
    }

    private static bool NeedsCharset(string type) =>
        type.StartsWith("text/", StringComparison.OrdinalIgnoreCase) || TextLike.Contains(type);
}
