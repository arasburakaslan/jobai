using JobRag.Domain.Common;

namespace JobRag.Domain.Users;

/// <summary>
/// A user within the system.
/// </summary>
public class User : BaseEntity
{
    public string Email { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public string Role { get; set; } = "User";

    // Navigation
    public UserProfile? Profile { get; set; }
    public ICollection<UserSavedJob> SavedJobs { get; set; } = new List<UserSavedJob>();
}
