using JobRag.Domain.Jobs;

namespace JobRag.Application.Abstractions;

/// <summary>
/// Checks whether a normalized job already exists in storage.
/// </summary>
public interface IJobDeduplicator
{
    Task<bool> IsDuplicateAsync(Job job, CancellationToken ct = default);
}