namespace JobRag.Application.Features.Search.DTOs;

/// <summary>
/// Request model for hybrid job search.
/// </summary>
public class SearchRequest
{
    /// <summary>
    /// Free-text search query (e.g., "Senior C# developer remote Azure").
    /// </summary>
    public string Query { get; set; } = default!;

    /// <summary>
    /// Filter by country code (e.g., "DE", "NL").
    /// </summary>
    public string? CountryCode { get; set; }

    /// <summary>
    /// Filter by industry (e.g., "IT", "Healthcare").
    /// </summary>
    public string? Industry { get; set; }

    /// <summary>
    /// Minimum salary filter.
    /// </summary>
    public int? MinSalary { get; set; }

    /// <summary>
    /// Filter for visa sponsorship availability.
    /// </summary>
    public bool? VisaSponsorship { get; set; }

    /// <summary>
    /// Filter for remote positions.
    /// </summary>
    public bool? Remote { get; set; }

    /// <summary>
    /// Weight for vector search score (0.0 to 1.0). Default 0.6.
    /// </summary>
    public float VectorWeight { get; set; } = 0.6f;

    /// <summary>
    /// Weight for full-text search score (0.0 to 1.0). Default 0.4.
    /// </summary>
    public float TextWeight { get; set; } = 0.4f;

    /// <summary>
    /// Number of results to return. Default 20.
    /// </summary>
    public int TopN { get; set; } = 20;

    /// <summary>
    /// Offset for pagination.
    /// </summary>
    public int Offset { get; set; } = 0;
}
