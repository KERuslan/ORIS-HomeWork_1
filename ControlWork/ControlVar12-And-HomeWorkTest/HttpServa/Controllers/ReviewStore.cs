namespace MyHttpServer.Controllers;

public sealed class GameReview
{
    public int Id { get; set; }
    public string Game { get; set; } = "";
    public int Score { get; set; }
    public string Status { get; set; } = "draft";
}

public sealed class ReviewStore
{
    private readonly List<GameReview> _reviews = new();
    private readonly object _lock = new();
    private int _nextId = 1;

    public (IReadOnlyList<GameReview> Items, decimal? Average) GetAll(int? minScore = null)
    {
        lock (_lock)
        {
            var items = _reviews
                .Where(x => !minScore.HasValue || x.Score >= minScore.Value)
                .OrderBy(x => x.Id)
                .ToList();

            decimal? average = items.Count == 0
                ? null
                : Math.Round(items.Average(x => (decimal)x.Score), 1, MidpointRounding.AwayFromZero);

            return (items.Select(Copy).ToList(), average);
        }
    }

    public GameReview? GetById(int id)
    {
        lock (_lock)
        {
            var item = _reviews.FirstOrDefault(x => x.Id == id);
            return item is null ? null : Copy(item);
        }
    }

    public GameReview Add(string game, int score)
    {
        lock (_lock)
        {
            var item = new GameReview
            {
                Id = _nextId++,
                Game = game,
                Score = score,
                Status = "draft"
            };
            _reviews.Add(item);
            return Copy(item);
        }
    }

    public bool Update(int id, string game, int score, string status)
    {
        lock (_lock)
        {
            var item = _reviews.FirstOrDefault(x => x.Id == id);
            if (item is null) return false;

            item.Game = game;
            item.Score = score;
            item.Status = status;
            return true;
        }
    }

    public static string? Validate(string? game, int score, string? status = "draft")
    {
        game = game?.Trim();
        if (string.IsNullOrWhiteSpace(game) || game.Length < 2 || game.Length > 80)
            return "Название игры должна содержать от 2 до 80 символов";
        if (score < 1 || score > 5)
            return "Оценка должна быть целым числом от 1 до 5";
        if (status is not ("draft" or "published"))
            return "Статус должен быть либо draft либо published";
        return null;
    }

    private static GameReview Copy(GameReview item) => new()
    {
        Id = item.Id,
        Game = item.Game,
        Score = item.Score,
        Status = item.Status
    };
}
