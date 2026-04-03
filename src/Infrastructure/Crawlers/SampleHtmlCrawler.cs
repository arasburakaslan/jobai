using HtmlAgilityPack;
using JobRag.Application.Abstractions;
using JobRag.Application.Features.Jobs.DTOs;
using Microsoft.Extensions.Logging;

namespace JobRag.Infrastructure.Crawlers;

/// <summary>
/// Example HTML scraping crawler using HtmlAgilityPack.
/// Adapt selectors for the target website. Respect robots.txt and rate limits.
/// </summary>
public class SampleHtmlCrawler : IJobCrawler
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SampleHtmlCrawler> _logger;

    public string SourceName => "SampleHTML";

    public SampleHtmlCrawler(HttpClient httpClient, ILogger<SampleHtmlCrawler> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IEnumerable<RawJob>> CrawlAsync(CancellationToken ct = default)
    {
        var jobs = new List<RawJob>();

        try
        {
            _logger.LogInformation("Starting HTML crawl from {Source}", SourceName);

            // Example: scrape a job listing page
            var baseUrl = "https://example.com/jobs"; // Replace with target
            var html = await _httpClient.GetStringAsync(baseUrl, ct);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Adapt CSS selectors to the actual website structure
            var jobNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'job-listing')]");
            if (jobNodes == null)
            {
                _logger.LogWarning("No job nodes found on {Source}", SourceName);
                return jobs;
            }

            foreach (var node in jobNodes)
            {
                var title = node.SelectSingleNode(".//h2")?.InnerText?.Trim() ?? "Unknown";
                var company = node.SelectSingleNode(".//span[@class='company']")?.InnerText?.Trim() ?? "Unknown";
                var location = node.SelectSingleNode(".//span[@class='location']")?.InnerText?.Trim() ?? "";
                var link = node.SelectSingleNode(".//a")?.GetAttributeValue("href", "") ?? "";
                var description = node.SelectSingleNode(".//div[@class='description']")?.InnerText?.Trim() ?? "";

                if (!string.IsNullOrWhiteSpace(link))
                {
                    // Make relative URLs absolute
                    if (link.StartsWith('/'))
                        link = $"https://example.com{link}";

                    jobs.Add(new RawJob
                    {
                        Title = title,
                        Company = company,
                        Location = location,
                        Description = description,
                        Url = link,
                        Source = SourceName,
                        PostedDate = null
                    });
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
