using JobRag.Application.Abstractions;
using JobRag.Domain.Jobs;
using Microsoft.Extensions.DependencyInjection;

namespace JobRag.Workers;

/// <summary>
/// Background worker that processes jobs pending embedding.
/// Fetches unembedded jobs in batches, generates embeddings, and saves them.
/// </summary>
public class EmbeddingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmbeddingWorker> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);
    private readonly int _batchSize = 50;

    public EmbeddingWorker(IServiceScopeFactory scopeFactory, ILogger<EmbeddingWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Embedding worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in embedding worker");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var jobRepo = scope.ServiceProvider.GetRequiredService<IJobRepository>();
        var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();

        var pendingJobs = await jobRepo.GetPendingEmbeddingAsync(_batchSize, ct);
        if (pendingJobs.Count == 0)
        {
            _logger.LogDebug("No jobs pending embedding");
            return;
        }

        _logger.LogInformation("Processing {Count} jobs for embedding", pendingJobs.Count);

        foreach (var job in pendingJobs)
        {
            try
            {
                // Embed title + first portion of description (cost optimization)
                var textToEmbed = $"{job.Title} {job.Company ?? ""} {job.Location ?? ""} " +
                                  (job.Description.Length > 2000
                                      ? job.Description[..2000]
                                      : job.Description);

                var vector = await embeddingService.EmbedTextAsync(textToEmbed, ct);

                job.Embedding = new JobEmbedding
                {
                    JobId = job.Id,
                    Embedding = vector
                };
                job.EmbeddingPending = false;

                await jobRepo.SaveChangesAsync(ct);
                _logger.LogDebug("Embedded job: {Title} ({Id})", job.Title, job.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to embed job {JobId}: {Title}", job.Id, job.Title);
                // Continue with next job — don't fail the entire batch
            }
        }

        _logger.LogInformation("Embedding batch complete");
    }
}
