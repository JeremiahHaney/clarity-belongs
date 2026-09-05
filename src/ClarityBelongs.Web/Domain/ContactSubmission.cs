namespace ClarityBelongs.Web.Domain;

public sealed class ContactSubmission
{
    public long Id { get; set; }
    public long? UserId { get; set; }
    public string Category { get; set; } = "General";
    public string Message { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public string? SourcePath { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
