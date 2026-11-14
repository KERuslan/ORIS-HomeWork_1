using System.Reflection;
using MyHttpServer.Framework.Attributes;

namespace MyHttpServer.Framework.Routing;

public sealed record ControllerRoute(Type Controller, MethodInfo Action);

public sealed class ControllerRoutes
{
    private readonly Dictionary<string, ControllerRoute> _routes = new(StringComparer.OrdinalIgnoreCase);

    public ControllerRoutes(params Type[] controllers)
    {
        foreach (Type controller in controllers)
        {
            if (!controller.IsDefined(typeof(ControllerAttribute)))
                continue;

            string name = controller.Name;
            const string suffix = "Controller";
            if (name.EndsWith(suffix, StringComparison.Ordinal))
                name = name[..^suffix.Length];

            foreach (var action in controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                string path = $"/{name}/{action.Name}";
                var route = new ControllerRoute(controller, action);
                if (action.IsDefined(typeof(GetAttribute))) _routes.Add($"GET {path}", route);
                if (action.IsDefined(typeof(PostAttribute))) _routes.Add($"POST {path}", route);
            }
        }
    }

    public ControllerRoute? Find(string method, string path)
    {
        return _routes.GetValueOrDefault($"{method} {path.TrimEnd('/')}");
    }
}
