using JobRag.Domain.Common;

namespace JobRag.Domain.Users;

/// <summary>
/// Tracks a user's saved/bookmarked job with match score and application status.
/// </summary>
public class UserSavedJob : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid JobId { get; set; }

    public string Status { get; set; } = "Saved"; // Saved, Applied, Rejected, Interview
    public float? MatchScore { get; set; }

    // Navigation
    public User User { get; set; } = default!;
    public Jobs.Job Job { get; set; } = default!;
}
