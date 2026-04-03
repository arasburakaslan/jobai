using JobRag.Application.Abstractions;
using JobRag.Application.Features.Search.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace JobRag.Api.Controllers;

/// <summary>
/// Hybrid search endpoint — the core RAG retrieval API.
/// Combines vector similarity + full-text search for intelligent job discovery.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly IHybridSearchService _searchService;
    private readonly ILogger<SearchController> _logger;

    public SearchController(IHybridSearchService searchService, ILogger<SearchController> logger)
    {
        _searchService = searchService;
        _logger = logger;
    }

    /// <summary>
    /// Execute a hybrid search for jobs.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<SearchResult>> Search(
        [FromBody] SearchRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return BadRequest(new { error = "Query is required" });
        }

        _logger.LogInformation("Search request: {Query}", request.Query);
        var result = await _searchService.SearchAsync(request, ct: ct);
        return Ok(result);
    }
}
