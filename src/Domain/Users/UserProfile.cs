using Pgvector;

namespace JobRag.Domain.Users;

/// <summary>
/// User profile with CV text, CV embedding vector, country, and preferences.
/// </summary>
public class UserProfile
{
    public Guid UserId { get; set; }
    public string CountryCode { get; set; } = "DE";
    public string? CVText { get; set; }
    public Vector? CVEmbedding { get; set; }
    public string? PreferencesJson { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = default!;
}
