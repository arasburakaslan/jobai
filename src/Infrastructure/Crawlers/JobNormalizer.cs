using System.Security.Cryptography;
using System.Text;
using JobRag.Application.Abstractions;
using JobRag.Application.Features.Jobs.DTOs;
using JobRag.Domain.Jobs;
using Microsoft.Extensions.Logging;

namespace JobRag.Infrastructure.Crawlers;

/// <summary>
/// Normalizes raw crawled job data into consistent domain entities.
/// Handles trimming, country resolution, and description hashing.
/// </summary>
public class JobNormalizer : IJobNormalizer
{
    private readonly ILogger<JobNormalizer> _logger;

    public JobNormalizer(ILogger<JobNormalizer> logger)
    {
        _logger = logger;
    }

    public Job Normalize(RawJob rawJob)
    {
        var job = new Job
        {
            Title = rawJob.Title.Trim(),
            Company = rawJob.Company?.Trim(),
            Location = rawJob.Location?.Trim(),
            Description = rawJob.Description,
            Url = rawJob.Url.Trim(),
            Source = rawJob.Source,
            PostedDate = rawJob.PostedDate,
            CountryCode = ResolveCountry(rawJob.Location),
            DescriptionHash = ComputeHash(rawJob.Description),
            EmbeddingPending = true
        };

        _logger.LogDebug("Normalized job: {Title} from {Source}", job.Title, job.Source);
        return job;
    }

    private static string ResolveCountry(string? location)
    {
        if (string.IsNullOrWhiteSpace(location))
            return "DE"; // Default to Germany

        var lower = location.ToLowerInvariant();

        if (lower.Contains("netherlands") || lower.Contains("nederland") ||
            lower.Contains("amsterdam") || lower.Contains("rotterdam") ||
            lower.Contains("den haag") || lower.Contains("utrecht"))
            return "NL";

        if (lower.Contains("austria") || lower.Contains("österreich") ||
            lower.Contains("wien") || lower.Contains("vienna"))
            return "AT";

        // Default to Germany
        return "DE";
    }

    private static string ComputeHash(string text)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
