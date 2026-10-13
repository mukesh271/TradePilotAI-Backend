namespace TradePilot.Domain.Entities;

public class News
{
    public long Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public NewsSentiment Sentiment { get; set; }

    public decimal RelevanceScore { get; set; }

    public DateTime PublishedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
