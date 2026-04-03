using JobRag.Domain.Common;

namespace JobRag.Domain.Jobs;

/// <summary>
/// Represents a job posting aggregated from external sources.
/// Jobs are global (not tenant-scoped) — they are shared across all tenants.
/// Country-aware for multi-country support.
/// </summary>
public class Job : BaseEntity
{
    public string CountryCode { get; set; } = "DE";
    public string? Industry { get; set; }

    public string Title { get; set; } = default!;
    public string? Company { get; set; }
    public string? Location { get; set; }
    public string Description { get; set; } = default!;

    public string Url { get; set; } = default!;
    public string? Source { get; set; }

    public int? SalaryMin { get; set; }
    public int? SalaryMax { get; set; }
    public string? Currency { get; set; }

    public string? LanguageRequired { get; set; }
    public bool? VisaSponsorship { get; set; }
    public bool? Remote { get; set; }

    public DateTime? PostedDate { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Indicates this job is pending embedding by the background worker.
    /// </summary>
    public bool EmbeddingPending { get; set; } = true;

    /// <summary>
    /// Hash of the description for fast change detection / deduplication.
    /// </summary>
    public string? DescriptionHash { get; set; }

    // Navigation
    public JobEmbedding? Embedding { get; set; }
}
