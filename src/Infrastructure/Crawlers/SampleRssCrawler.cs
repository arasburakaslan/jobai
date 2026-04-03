using System.Xml.Linq;
using JobRag.Application.Abstractions;
using JobRag.Application.Features.Jobs.DTOs;
using Microsoft.Extensions.Logging;

namespace JobRag.Infrastructure.Crawlers;

/// <summary>
/// Example RSS-based job crawler. This is a template you can adapt for real job board RSS feeds.
/// For MVP, this demonstrates the crawler pattern.
/// </summary>
public class SampleRssCrawler : IJobCrawler
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SampleRssCrawler> _logger;
    private readonly string _feedUrl;

    public string SourceName => "SampleRSS";

    public SampleRssCrawler(HttpClient httpClient, ILogger<SampleRssCrawler> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _feedUrl = "https://example.com/jobs/rss"; // Replace with real RSS feed URL
    }

    public async Task<IEnumerable<RawJob>> CrawlAsync(CancellationToken ct = default)
    {
        var jobs = new List<RawJob>();

        try
        {
            _logger.LogInformation("Starting RSS crawl from {Source}", SourceName);

            var response = await _httpClient.GetStringAsync(_feedUrl, ct);
            var doc = XDocument.Parse(response);

            var items = doc.Descendants("item");
            foreach (var item in items)
            {
                var rawJob = new RawJob
                {
                    Title = item.Element("title")?.Value ?? "Unknown",
                    Company = item.Element("company")?.Value ?? "Unknown",
                    Location = item.Element("location")?.Value ?? "",
                    Description = item.Element("description")?.Value ?? "",
                    Url = item.Element("link")?.Value ?? "",
                    Source = SourceName,
                    PostedDate = DateTime.TryParse(item.Element("pubDate")?.Value, out var dt) ? dt : null
                };

                if (!string.IsNullOrWhiteSpace(rawJob.Url))
                {
                    jobs.Add(rawJob);
                }
            }

            _logger.LogInformation("Crawled {Count} jobs from {Source}", jobs.Count, SourceName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error crawling {Source}", SourceName);
        }

        return jobs;
    }
}
