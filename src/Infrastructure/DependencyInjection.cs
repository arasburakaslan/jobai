using JobRag.Application.Abstractions;
using JobRag.Infrastructure.Crawlers;
using JobRag.Infrastructure.Embeddings;
using JobRag.Infrastructure.Persistence;
using JobRag.Infrastructure.Persistence.Repositories;
using JobRag.Infrastructure.Search;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pgvector.EntityFrameworkCore;

namespace JobRag.Infrastructure;

/// <summary>
/// Registers all Infrastructure services with the DI container.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("Default"),
                npgsqlOptions =>
                {
                    npgsqlOptions.UseVector();
                    npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                });
        });

        // Repositories
        services.AddScoped<IJobRepository, JobRepository>();

        // Crawlers
        services.AddScoped<IJobNormalizer, JobNormalizer>();
        services.AddScoped<IJobDeduplicator, JobDeduplicator>();
        services.AddScoped<IJobMetadataExtractor, JobMetadataExtractor>();

        // Register crawler implementations
        services.AddHttpClient<SampleRssCrawler>();
        services.AddHttpClient<SampleHtmlCrawler>();
        services.AddScoped<IJobCrawler, SampleRssCrawler>();

        // Embedding service
        services.AddHttpClient<OpenAIEmbeddingService>();
        services.AddScoped<IEmbeddingService, OpenAIEmbeddingService>();

        // Hybrid search
        services.AddScoped<IHybridSearchService, HybridSearchService>();

        return services;
    }
}
