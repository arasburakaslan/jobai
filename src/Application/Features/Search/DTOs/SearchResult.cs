namespace JobRag.Application.Features.Search.DTOs;

/// <summary>
/// Result of a hybrid search query.
/// </summary>
public class SearchResult
{
    public IReadOnlyList<JobSearchResult> Jobs { get; set; } = [];
    public int TotalCount { get; set; }
    public string Query { get; set; } = default!;
}

/// <summary>
/// A single job result with its relevance scores.
/// </summary>
public class JobSearchResult
{
    public Guid JobId { get; set; }
    public string Title { get; set; } = default!;
    public string? Company { get; set; }
    public string? Location { get; set; }
    public string CountryCode { get; set; } = default!;
    public string? Industry { get; set; }
    public string? Source { get; set; }
    public string Url { get; set; } = default!;

    public int? SalaryMin { get; set; }
    public int? SalaryMax { get; set; }
    public string? Currency { get; set; }
    public bool? Remote { get; set; }
    public bool? VisaSponsorship { get; set; }
    public string? LanguageRequired { get; set; }

    public DateTime? PostedDate { get; set; }

    /// <summary>
    /// Combined hybrid relevance score.
    /// </summary>
    public float RelevanceScore { get; set; }

    /// <summary>
    /// Vector similarity score component.
    /// </summary>
    public float VectorScore { get; set; }

    /// <summary>
    /// Full-text search score component.
    /// </summary>
    public float TextScore { get; set; }

    /// <summary>
    /// Short snippet of the description for display.
    /// </summary>
    public string? DescriptionSnippet { get; set; }
}
