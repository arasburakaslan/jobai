using Pgvector;

namespace JobRag.Domain.Jobs;

/// <summary>
/// Stores the vector embedding for a job posting.
/// Separated from the Job entity for flexibility and performance.
/// </summary>
public class JobEmbedding
{
    public Guid JobId { get; set; }
    public Vector Embedding { get; set; } = default!;

    // Navigation
    public Job Job { get; set; } = default!;
}
