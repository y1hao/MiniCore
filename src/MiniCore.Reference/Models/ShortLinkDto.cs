namespace MiniCore.Reference.Models;

public class ShortLinkDto
{
    public int Id { get; set; }
    public string ShortCode { get; set; } = string.Empty;
    public string OriginalUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string ShortUrl { get; set; } = string.Empty;
}

public class CreateShortLinkRequest
{
    public string OriginalUrl { get; set; } = string.Empty;
    public string? ShortCode { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

