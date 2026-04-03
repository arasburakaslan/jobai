using JobRag.Application.Features.Jobs.DTOs;
using JobRag.Domain.Jobs;

namespace JobRag.Application.Abstractions;

/// <summary>
/// Normalizes raw crawled job data into a consistent domain entity.
/// </summary>
public interface IJobNormalizer
{
    Job Normalize(RawJob rawJob);
}
