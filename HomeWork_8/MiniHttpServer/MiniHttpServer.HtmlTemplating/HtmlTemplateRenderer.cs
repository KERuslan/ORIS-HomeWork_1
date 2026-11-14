using System.Collections;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace MyHttpServer.HtmlTemplating;

public sealed class HtmlTemplateRenderer : IHtmlTemplateRenderer
{
    private static readonly Regex Tokens = new(
        @"\$\{([^{}]+)\}|\$(if|foreach)\s*\(([^()]*)\)|\$(else|endif|endfor)\b|\$(if|foreach)\b|\$\{",
        RegexOptions.CultureInvariant);

    private static readonly Regex Loop = new(
        @"^\s*var\s+([A-Za-z_]\w*)\s+in\s+([A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*)\s*$",
        RegexOptions.CultureInvariant);

    public string RenderFromString(string htmlTemplate, object dataModel)
    {
        ArgumentNullException.ThrowIfNull(htmlTemplate);
        ArgumentNullException.ThrowIfNull(dataModel);
        var parts = new Parser(htmlTemplate).Parse();
        var result = new StringBuilder();
        Render(parts, new Scope(dataModel), result);
        return result.ToString();
    }

    public string RenderFromFile(string filePath, object dataModel) =>
        RenderFromString(File.ReadAllText(filePath), dataModel);

    public string RenderToFile(string templatePath, string outputPath, object dataModel)
    {
        string result = RenderFromFile(templatePath, dataModel);
        File.WriteAllText(outputPath, result);
        return result;
    }

    private static void Render(IReadOnlyList<Part> parts, Scope scope, StringBuilder result)
    {
        foreach (Part part in parts)
        {
            switch (part)
            {
                case Text text:
                    result.Append(text.Value);
                    break;
                case Variable variable:
                    result.Append(WebUtility.HtmlEncode(Resolve(scope, variable.Path)?.ToString()));
                    break;
                case Conditional conditional:
                    if (Resolve(scope, conditional.Path) is not bool condition)
                        throw new FormatException($"Условие {conditional.Path} должно быть логическим.");
                    Render(condition ? conditional.Yes : conditional.No, scope, result);
                    break;
                case Each each:
                    object? value = Resolve(scope, each.Path);
                    if (value is null) break;
                    if (value is string || value is not IEnumerable items)
                        throw new FormatException($"Значение {each.Path} должно быть коллекцией.");
                    foreach (object? item in items)
                        Render(each.Body, new Scope(scope.Model, each.Name, item, scope), result);
                    break;
            }
        }
    }

    private static object? Resolve(Scope scope, string path)
    {
        string[] names = path.Split('.');
        object? value = scope.Model;
        int start = 0;
        for (Scope? local = scope; local is not null; local = local.Parent)
        {
            if (local.Name != names[0]) continue;
            value = local.Item;
            start = 1;
            break;
        }

        for (int i = start; i < names.Length; i++)
        {
            if (value is null) return null;
            PropertyInfo? property = value.GetType().GetProperty(names[i], BindingFlags.Instance | BindingFlags.Public);
            if (property is null || property.GetIndexParameters().Length != 0)
                throw new FormatException($"Свойство {path} не найдено.");
            value = property.GetValue(value);
        }
        return value;
    }

    private sealed record Scope(object Model, string? Name = null, object? Item = null, Scope? Parent = null);
    private abstract record Part;
    private sealed record Text(string Value) : Part;
    private sealed record Variable(string Path) : Part;
    private sealed record Conditional(string Path, List<Part> Yes, List<Part> No) : Part;
    private sealed record Each(string Name, string Path, List<Part> Body) : Part;

    private sealed class Parser(string template)
    {
        private int _offset;

        public List<Part> Parse() => ReadBlock(out _, 0);

        private List<Part> ReadBlock(out string end, int depth, params string[] stops)
        {
            if (depth > 64) throw new FormatException("Слишком глубокая вложенность шаблона.");
            var parts = new List<Part>();
            end = "";
            for (Match token = Tokens.Match(template, _offset); token.Success; token = Tokens.Match(template, _offset))
            {
                if (token.Index > _offset)
                    parts.Add(new Text(template[_offset..token.Index]));
                _offset = token.Index + token.Length;

                if (token.Groups[1].Success)
                {
                    parts.Add(new Variable(ReadPath(token.Groups[1].Value)));
                    continue;
                }
                if (token.Groups[4].Success)
                {
                    end = token.Groups[4].Value;
                    if (stops.Contains(end)) return parts;
                    throw new FormatException($"Неожиданный маркер ${end}.");
                }
                if (token.Groups[2].Value == "if")
                {
                    string path = ReadPath(token.Groups[3].Value);
                    var yes = ReadBlock(out string branch, depth + 1, "else", "endif");
                    var no = branch == "else" ? ReadBlock(out _, depth + 1, "endif") : [];
                    parts.Add(new Conditional(path, yes, no));
                    continue;
                }
                if (token.Groups[2].Value == "foreach")
                {
                    Match loop = Loop.Match(token.Groups[3].Value);
                    if (!loop.Success) throw new FormatException("Некорректный $foreach.");
                    var body = ReadBlock(out _, depth + 1, "endfor");
                    parts.Add(new Each(loop.Groups[1].Value, loop.Groups[2].Value, body));
                    continue;
                }
                throw new FormatException($"Незавершённая конструкция на позиции {token.Index}.");
            }
            if (stops.Length > 0) throw new FormatException($"Ожидается маркер ${stops[^1]}.");
            if (_offset < template.Length) parts.Add(new Text(template[_offset..]));
            return parts;
        }

        private static string ReadPath(string expression)
        {
            string path = expression.Trim();
            if (!Regex.IsMatch(path, @"^[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*$"))
                throw new FormatException($"Некорректное имя свойства: {expression}.");
            return path;
        }
    }
}
