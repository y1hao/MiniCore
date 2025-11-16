using System.ComponentModel.DataAnnotations;

namespace MiniCore.Reference.Models;

public class ShortLink
{
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    public string ShortCode { get; set; } = string.Empty;

    [Required]
    [Url]
    public string OriginalUrl { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ExpiresAt { get; set; }
}

