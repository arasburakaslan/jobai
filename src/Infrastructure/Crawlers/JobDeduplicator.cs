using JobRag.Application.Abstractions;
using JobRag.Domain.Jobs;

namespace JobRag.Infrastructure.Crawlers;

/// <summary>
/// Performs simple URL and description-hash based deduplication.
/// </summary>
public class JobDeduplicator : IJobDeduplicator
{
    private readonly IJobRepository _jobRepository;

    public JobDeduplicator(IJobRepository jobRepository)
    {
        _jobRepository = jobRepository;
    }

    public async Task<bool> IsDuplicateAsync(Job job, CancellationToken ct = default)
    {
        if (await _jobRepository.GetByUrlAsync(job.Url, ct) is not null)
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(job.DescriptionHash) &&
            await _jobRepository.GetByDescriptionHashAsync(job.DescriptionHash, ct) is not null)
        {
            return true;
        }

        return false;
    }
}