using JobRag.Application.Features.Search.DTOs;
using Pgvector;

namespace JobRag.Application.Abstractions;

/// <summary>
/// Hybrid search service combining vector similarity + full-text BM25 search.
/// </summary>
public interface IHybridSearchService
{
    /// <summary>
    /// Search jobs using a combination of vector similarity and full-text search.
    /// </summary>
    /// <param name="request">The search request with query, filters, and pagination.</param>
    /// <param name="userEmbedding">Optional user CV embedding for personalized ranking.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<SearchResult> SearchAsync(SearchRequest request, Vector? userEmbedding = null, CancellationToken ct = default);
}
