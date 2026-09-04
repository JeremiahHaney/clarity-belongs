namespace ClarityBelongs.Web.Domain;

public sealed class FeedbackSubmission
{
    public long Id { get; set; }

    public string Kind { get; set; } = "general";

    public string Message { get; set; } = string.Empty;

    public string? ProductSlug { get; set; }

    public string? Path { get; set; }

    public string? Contact { get; set; }

    public DateTime CreatedUtc { get; set; }
}
