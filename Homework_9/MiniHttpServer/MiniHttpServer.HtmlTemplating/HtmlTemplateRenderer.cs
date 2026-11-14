using System.Collections;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace MiniHttpServer.HtmlTemplating;

public sealed class HtmlTemplateRenderer : IHtmlTemplateRenderer
{
    private static readonly Regex Token = new(
        @"\$foreach\(\s*var\s+(?<alias>\w+)\s+in\s+(?<items>\w+(?:\.\w+)*)\s*\)|(?<end>\$endfor)|\$\{(?<value>\w+(?:\.\w+)*)\}");

    public string Render(string template, object model)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(model);
        var tokens = Token.Matches(template);
        var closing = FindClosingTokens(tokens);
        var output = new StringBuilder();
        RenderSection(template, tokens, closing, 0, tokens.Count, 0, template.Length, model,
            new Dictionary<string, object?>(), output);
        return output.ToString();
    }

    private static Dictionary<int, int> FindClosingTokens(MatchCollection tokens)
    {
        var loops = new Stack<int>();
        var closing = new Dictionary<int, int>();
        for (int i = 0; i < tokens.Count; i++)
        {
            if (tokens[i].Groups["alias"].Success)
                loops.Push(i);
            if (!tokens[i].Groups["end"].Success)
                continue;
            if (!loops.TryPop(out int start))
                throw new FormatException("У $endfor нет соответствующего $foreach.");

            closing.Add(start, i);
        }

        if (loops.Count > 0)
            throw new FormatException("Отсутствует $endfor.");

        return closing;
    }

    private static void RenderSection(string template, MatchCollection tokens, Dictionary<int, int> closing,
        int first, int last, int position, int end, object model, Dictionary<string, object?> variables,
        StringBuilder output)
    {
        for (int i = first; i < last; i++)
        {
            Match token = tokens[i];
            output.Append(template, position, token.Index - position);
            position = token.Index + token.Length;
            if (token.Groups["value"].Success)
            {
                object? value = Resolve(token.Groups["value"].Value, model, variables);
                output.Append(WebUtility.HtmlEncode(Convert.ToString(value, CultureInfo.CurrentCulture)));
                continue;
            }

            string alias = token.Groups["alias"].Value;
            object? items = Resolve(token.Groups["items"].Value, model, variables);
            int close = closing[i];
            if (items is IEnumerable collection && items is not string)
            {
                bool hadVariable = variables.TryGetValue(alias, out object? previous);
                foreach (object? item in collection)
                {
                    variables[alias] = item;
                    RenderSection(template, tokens, closing, i + 1, close, position, tokens[close].Index,
                        model, variables, output);
                }

                if (hadVariable)
                    variables[alias] = previous;
                else
                    variables.Remove(alias);
            }
            else if (items is not null)
            {
                throw new FormatException($"Выражение {token.Groups["items"].Value} не является коллекцией.");
            }

            i = close;
            position = tokens[close].Index + tokens[close].Length;
        }

        output.Append(template, position, end - position);
    }

    private static object? Resolve(string path, object model, Dictionary<string, object?> variables)
    {
        string[] parts = path.Split('.');
        object? value = model;
        int first = 0;
        if (variables.TryGetValue(parts[0], out var variable))
        {
            value = variable;
            first = 1;
        }

        for (int i = first; i < parts.Length && value is not null; i++)
        {
            var property = value.GetType().GetProperty(parts[i]);
            if (property?.GetMethod?.IsPublic != true || property.GetIndexParameters().Length > 0)
                throw new FormatException($"Свойство шаблона не найдено: {path}.");

            value = property.GetValue(value);
        }

        return value;
    }
}
