using JobRag.Domain.Jobs;

namespace JobRag.Application.Abstractions;

/// <summary>
/// Extracts structured metadata from a normalized job posting.
/// </summary>
public interface IJobMetadataExtractor
{
    Task<Job> ExtractMetadataAsync(Job job, CancellationToken ct = default);
}