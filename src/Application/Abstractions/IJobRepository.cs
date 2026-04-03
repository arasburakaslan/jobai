using JobRag.Domain.Jobs;

namespace JobRag.Application.Abstractions;

/// <summary>
/// Repository for managing Job entities.
/// </summary>
public interface IJobRepository
{
    Task<Job?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Job?> GetByUrlAsync(string url, CancellationToken ct = default);
    Task<Job?> GetByDescriptionHashAsync(string descriptionHash, CancellationToken ct = default);
    Task<IReadOnlyList<Job>> GetPendingEmbeddingAsync(int batchSize, CancellationToken ct = default);
    Task AddAsync(Job job, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
