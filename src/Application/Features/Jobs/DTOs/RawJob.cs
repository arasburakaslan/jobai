namespace JobRag.Application.Features.Jobs.DTOs;

/// <summary>
/// Raw job data as returned from a crawler before normalization.
/// </summary>
public class RawJob
{
    public string Title { get; set; } = default!;
    public string Company { get; set; } = default!;
    public string Location { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string Url { get; set; } = default!;
    public string Source { get; set; } = default!;
    public DateTime? PostedDate { get; set; }
}
