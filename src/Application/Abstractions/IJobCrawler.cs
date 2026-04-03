using JobRag.Application.Features.Jobs.DTOs;

namespace JobRag.Application.Abstractions;

/// <summary>
/// Crawls a specific job source and returns raw job data.
/// Each concrete crawler targets a specific board/API/RSS feed.
/// </summary>
public interface IJobCrawler
{
    /// <summary>
    /// The name of this crawler source (e.g., "Stepstone", "LinkedIn RSS").
    /// </summary>
    string SourceName { get; }

    /// <summary>
    /// Crawl the source and return raw job listings.
    /// </summary>
    Task<IEnumerable<RawJob>> CrawlAsync(CancellationToken ct = default);
}
