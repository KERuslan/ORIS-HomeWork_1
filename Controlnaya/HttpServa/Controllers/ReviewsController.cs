using System.Globalization;
using System.Net;
using System.Text;
using MyHttpServer.Core.Handlers;

namespace MyHttpServer.Controllers;

public sealed class ReviewsController
{
    private readonly ResponseWriter _writer;
    private readonly ReviewStore _store;

    public ReviewsController(ResponseWriter writer) : this(writer, new ReviewStore()) { }

    public ReviewsController(ResponseWriter writer, ReviewStore store)
    {
        _writer = writer;
        _store = store;
    }

    public async Task List(HttpListenerContext context)
    {
        int? minScore = null;
        string? raw = context.Request.QueryString["minScore"];
        if (!string.IsNullOrWhiteSpace(raw))
        {
            if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) || n < 1 || n > 5)
            {
                await Error(context, 400, "minScore должен быть целым числом от 1 до 5");
                return;
            }
            minScore = n;
        }

        var result = _store.GetAll(minScore);
        var rows = new StringBuilder();
        foreach (var item in result.Items)
        {
            rows.Append($"<tr><td>{item.Id}</td><td>{WebUtility.HtmlEncode(item.Game)}</td><td>{item.Score}</td><td>{item.Status}</td><td><a href=\"/reviews/details?id={item.Id}\">Открыть</a></td></tr>");
        }

        string average = result.Average?.ToString("0.0", CultureInfo.InvariantCulture) ?? "нет данных";
        string content = $$"""
            <h1>Рейтинг компьютерных игр</h1>
            <form method="get" action="/reviews" class="filter">
              <label>Минимальная оценка
                <select name="minScore">
                  <option value="">Все</option>
                  {{Options(minScore)}}
                </select>
              </label>
              <button>Показать</button>
            </form>
            <p>Сколько записей: {{result.Items.Count}}. Средняя оценка: <b>{{average}}</b></p>
            <table><thead><tr><th>ID</th><th>Игра</th><th>Оценка</th><th>Статус</th><th></th></tr></thead>
            <tbody>{{(rows.Length == 0 ? "<tr><td colspan=\"5\">Записей пока нет</td></tr>" : rows.ToString())}}</tbody></table>
            <h2>Добавить оценку</h2>
            <form method="post" action="/reviews/create">
              <label>Название игры <input name="game" minlength="2" maxlength="80" required></label>
              <label>Оценка <input name="score" type="number" min="1" max="5" required></label>
              <button type="submit">Добавить</button>
            </form>
            """;

        await Page(context, "Рейтинг компьютерных игр", content);
    }

    public async Task Details(HttpListenerContext context)
    {
        if (!ReadId(context, out int id))
        {
            await Error(context, 400, "Укажите корректный id");
            return;
        }

        var item = _store.GetById(id);
        if (item is null)
        {
            await Error(context, 404, "Оценка не найдена");
            return;
        }

        string game = WebUtility.HtmlEncode(item.Game);
        string content = $$"""
            <h1>Оценка игры</h1>
            <p>ID: {{item.Id}}</p>
            <form id="editForm">
              <label>Название игры <input name="game" value="{{game}}" minlength="2" maxlength="80" required></label>
              <label>Оценка <input name="score" type="number" min="1" max="5" value="{{item.Score}}" required></label>
              <label>Статус
                <select name="status">
                  <option value="draft" {{(item.Status == "draft" ? "selected" : "")}}>draft</option>
                  <option value="published" {{(item.Status == "published" ? "selected" : "")}}>published</option>
                </select>
              </label>
              <button type="submit">Сохранить</button>
            </form>
            <p id="result"></p>
            <p><a href="/reviews">К списку</a></p>
            <script>
            document.getElementById('editForm').addEventListener('submit', async e => {
              e.preventDefault();
              const form = e.currentTarget;
              const response = await fetch('/reviews/update?id={{item.Id}}', {
                method: 'PUT',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: new URLSearchParams(new FormData(form))
              });
              document.getElementById('result').textContent = response.status === 204
                ? 'Изменения сохранены'
                : 'Ошибка: ' + response.status;
            });
            </script>
            """;

        await Page(context, "Оценка игры", content);
    }

    public async Task Create(HttpListenerContext context)
    {
        var form = await ReadForm(context.Request);
        if (form is null || !form.TryGetValue("game", out string? game) ||
            !form.TryGetValue("score", out string? rawScore) ||
            !int.TryParse(rawScore, NumberStyles.Integer, CultureInfo.InvariantCulture, out int score))
        {
            await Error(context, 400, "Передайте game и score в формате формы");
            return;
        }

        string? error = ReviewStore.Validate(game, score);
        if (error is not null)
        {
            await Error(context, 400, error);
            return;
        }

        _store.Add(game.Trim(), score);
        _writer.Redirect(context, "/reviews", 303);
    }

    public async Task Update(HttpListenerContext context)
    {
        if (!ReadId(context, out int id))
        {
            await Error(context, 400, "Укажите корректный id");
            return;
        }

        if (_store.GetById(id) is null)
        {
            await Error(context, 404, "Оценка не найдена");
            return;
        }

        var form = await ReadForm(context.Request);
        if (form is null || !form.TryGetValue("game", out string? game) ||
            !form.TryGetValue("score", out string? rawScore) ||
            !form.TryGetValue("status", out string? status) ||
            !int.TryParse(rawScore, NumberStyles.Integer, CultureInfo.InvariantCulture, out int score))
        {
            await Error(context, 400, "Передайте game, score и status в формате формы");
            return;
        }

        game = game.Trim();
        string? error = ReviewStore.Validate(game, score, status);
        if (error is not null)
        {
            await Error(context, 400, error);
            return;
        }

        _store.Update(id, game, score, status);
        await _writer.SendTextAsync(context, "", statusCode: 204);
    }

    public async Task MethodNotAllowed(HttpListenerContext context, string allow)
    {
        context.Response.Headers["Allow"] = allow;
        await _writer.SendTextAsync(context, "405 Method Not Allowed", "text/plain; charset=utf-8", 405);
    }

    private async Task Error(HttpListenerContext context, int code, string message)
    {
        await Page(context, "Ошибка", $"<h1>{code}</h1><p>{WebUtility.HtmlEncode(message)}</p><p><a href=\"/reviews\">К списку</a></p>", code);
    }

    private Task Page(HttpListenerContext context, string title, string content, int status = 200)
    {
        string html = $$"""
            <!doctype html>
            <html lang="ru">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>{{WebUtility.HtmlEncode(title)}}</title>
              <style>
                body { max-width: 900px; margin: 35px auto; padding: 0 18px; font: 16px Arial, sans-serif; color: #222; }
                form { display: flex; flex-wrap: wrap; gap: 12px; align-items: end; margin: 18px 0; }
                label { display: grid; gap: 5px; }
                input, select, button { padding: 8px; font: inherit; }
                button { cursor: pointer; }
                table { width: 100%; border-collapse: collapse; margin: 18px 0; }
                th, td { text-align: left; border-bottom: 1px solid #ddd; padding: 9px; }
              </style>
            </head>
            <body>{{content}}</body>
            </html>
            """;
        return _writer.SendTextAsync(context, html, statusCode: status);
    }

    private static bool ReadId(HttpListenerContext context, out int id) =>
        int.TryParse(context.Request.QueryString["id"], NumberStyles.Integer, CultureInfo.InvariantCulture, out id) && id > 0;

    private static string Options(int? selected)
    {
        var result = new StringBuilder();
        for (int i = 1; i <= 5; i++)
            result.Append($"<option value=\"{i}\" {(selected == i ? "selected" : "")}>{i}</option>");
        return result.ToString();
    }

    private static async Task<Dictionary<string, string>?> ReadForm(HttpListenerRequest request)
    {
        if (request.ContentType?.StartsWith("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase) != true)
            return null;

        using var reader = new StreamReader(request.InputStream, request.ContentEncoding, false, 1024, true);
        string body = await reader.ReadToEndAsync();
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string part in body.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] pair = part.Split('=', 2);
            string key = WebUtility.UrlDecode(pair[0]) ?? "";
            string value = pair.Length > 1 ? WebUtility.UrlDecode(pair[1]) ?? "" : "";
            values[key] = value;
        }
        return values;
    }
}
