using JobRag.Domain.Common;

namespace JobRag.Domain.Users;

/// <summary>
/// Tracks job applications made by a user.
/// </summary>
public class JobApplication : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid JobId { get; set; }

    public DateTime? AppliedAt { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Submitted, Interviewing, Offered, Rejected
    public string? Notes { get; set; }
    public string? CoverLetterText { get; set; }

    // Navigation
    public User User { get; set; } = default!;
    public Jobs.Job Job { get; set; } = default!;
}
