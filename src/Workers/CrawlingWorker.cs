using JobRag.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace JobRag.Workers;

/// <summary>
/// Background worker that orchestrates job crawling from all registered sources.
/// Crawls → Normalizes → Deduplicates → Extracts metadata → Saves to DB.
/// </summary>
public class CrawlingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CrawlingWorker> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(6); // Crawl every 6 hours

    public CrawlingWorker(IServiceScopeFactory scopeFactory, ILogger<CrawlingWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Crawling worker started");

        // Initial delay to let the system warm up
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CrawlAllSourcesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in crawling worker");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task CrawlAllSourcesAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var crawlers = scope.ServiceProvider.GetServices<IJobCrawler>();
        var normalizer = scope.ServiceProvider.GetRequiredService<IJobNormalizer>();
        var deduplicator = scope.ServiceProvider.GetRequiredService<IJobDeduplicator>();
        var metadataExtractor = scope.ServiceProvider.GetRequiredService<IJobMetadataExtractor>();
        var jobRepo = scope.ServiceProvider.GetRequiredService<IJobRepository>();

        var totalNew = 0;
        var totalSkipped = 0;

        foreach (var crawler in crawlers)
        {
            try
            {
                _logger.LogInformation("Starting crawl: {Source}", crawler.SourceName);

                var rawJobs = await crawler.CrawlAsync(ct);

                foreach (var rawJob in rawJobs)
                {
                    try
                    {
                        // 1. Normalize
                        var job = normalizer.Normalize(rawJob);

                        // 2. Deduplicate
                        if (await deduplicator.IsDuplicateAsync(job, ct))
                        {
                            totalSkipped++;
                            continue;
                        }

                        // 3. Extract metadata
                        job = await metadataExtractor.ExtractMetadataAsync(job, ct);

                        // 4. Save
                        await jobRepo.AddAsync(job, ct);
                        await jobRepo.SaveChangesAsync(ct);
                        totalNew++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to process job: {Url}", rawJob.Url);
                    }
                }

                _logger.LogInformation("Crawl complete for {Source}", crawler.SourceName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Crawler failed: {Source}", crawler.SourceName);
            }
        }

        _logger.LogInformation("Crawl cycle complete. New: {New}, Skipped: {Skipped}", totalNew, totalSkipped);
    }
}
