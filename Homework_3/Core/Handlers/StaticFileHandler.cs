using System.Net;

namespace MyHttpServer.Core.Handlers;

public sealed class StaticFileHandler : IRequestHandler
{
    private static readonly StringComparison PathComparison =
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    private readonly string _root;
    private readonly string _mainSite;
    private readonly ResponseWriter _writer;

    public StaticFileHandler(string root, string mainSite, ResponseWriter writer)
    {
        _root = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        _mainSite = mainSite;
        _writer = writer;
    }

    public async Task<bool> HandleAsync(HttpListenerContext context)
    {
        string urlPath = context.Request.Url?.LocalPath ?? "/";
        string relative = urlPath.TrimStart('/');
        if (relative.Length == 0) relative = _mainSite + "/";

        string? full = ResolveSafe(relative);
        if (full is null) return false;

        if (Directory.Exists(full))
        {
            if (!urlPath.EndsWith('/') && urlPath != "/")
            {
                _writer.Redirect(context, urlPath + "/" + context.Request.Url?.Query);
                return true;
            }

            string folder = Path.GetFileName(full.TrimEnd(Path.DirectorySeparatorChar));
            full = Path.Combine(full, folder + ".html");
        }

        if (!File.Exists(full)) return false;

        await _writer.SendFileAsync(context, full);
        return true;
    }

    private string? ResolveSafe(string relative)
    {
        try
        {
            string full = Path.GetFullPath(Path.Combine(_root, relative));
            return full.StartsWith(_root, PathComparison) ? full : null;
        }
        catch (Exception e) when (e is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }
    }
}
