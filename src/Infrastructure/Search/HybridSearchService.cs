using JobRag.Application.Abstractions;
using JobRag.Application.Features.Search.DTOs;
using JobRag.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pgvector;

namespace JobRag.Infrastructure.Search;

/// <summary>
/// Production-grade hybrid search combining vector similarity (pgvector) and full-text search (ts_rank).
/// FinalScore = VectorWeight * VectorScore + TextWeight * TextScore
/// </summary>
public class HybridSearchService : IHybridSearchService
{
    private readonly ApplicationDbContext _db;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<HybridSearchService> _logger;

    public HybridSearchService(
        ApplicationDbContext db,
        IEmbeddingService embeddingService,
        ILogger<HybridSearchService> logger)
    {
        _db = db;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    public async Task<SearchResult> SearchAsync(
        SearchRequest request,
        Vector? userEmbedding = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Hybrid search: query='{Query}', country={Country}, topN={TopN}",
            request.Query, request.CountryCode, request.TopN);

        // Step 1: Generate query embedding
        var queryEmbedding = userEmbedding ?? await _embeddingService.EmbedTextAsync(request.Query, ct);

        // Step 2: Execute hybrid search via raw SQL for optimal performance
        var results = await ExecuteHybridSearchAsync(request, queryEmbedding, ct);

        return new SearchResult
        {
            Jobs = results,
            TotalCount = results.Count,
            Query = request.Query
        };
    }

    private async Task<IReadOnlyList<JobSearchResult>> ExecuteHybridSearchAsync(
        SearchRequest request,
        Vector queryEmbedding,
        CancellationToken ct)
    {
        // Build the hybrid SQL query combining vector + full-text search
        // Using parameterized query to prevent SQL injection
        var sql = @"
            SELECT
                j.""Id"" AS JobId,
                j.""Title"",
                j.""Company"",
                j.""Location"",
                j.""CountryCode"",
                j.""Industry"",
                j.""Source"",
                j.""Url"",
                j.""SalaryMin"",
                j.""SalaryMax"",
                j.""Currency"",
                j.""Remote"",
                j.""VisaSponsorship"",
                j.""LanguageRequired"",
                j.""PostedDate"",
                LEFT(j.""Description"", 300) AS DescriptionSnippet,
                ts_rank(
                    to_tsvector('english', COALESCE(j.""Title"",'') || ' ' || COALESCE(j.""Description"",''))
                    , plainto_tsquery('english', {0})
                ) AS TextScore,
                1.0 - (e.""Embedding"" <=> {1}::vector) AS VectorScore,
                ({2} * (1.0 - (e.""Embedding"" <=> {1}::vector)) + {3} * ts_rank(
                    to_tsvector('english', COALESCE(j.""Title"",'') || ' ' || COALESCE(j.""Description"",''))
                    , plainto_tsquery('english', {0})
                )) AS RelevanceScore
            FROM ""Jobs"" j
            INNER JOIN ""JobEmbeddings"" e ON j.""Id"" = e.""JobId""
            WHERE 1=1";

        var parameters = new List<object>
        {
            request.Query,          // {0}
            queryEmbedding,         // {1}
            request.VectorWeight,   // {2}
            request.TextWeight      // {3}
        };

        var paramIdx = 4;

        if (!string.IsNullOrEmpty(request.CountryCode))
        {
            sql += $@" AND j.""CountryCode"" = {{{paramIdx}}}";
            parameters.Add(request.CountryCode);
            paramIdx++;
        }

        if (!string.IsNullOrEmpty(request.Industry))
        {
            sql += $@" AND j.""Industry"" = {{{paramIdx}}}";
            parameters.Add(request.Industry);
            paramIdx++;
        }

        if (request.MinSalary.HasValue)
        {
            sql += $@" AND j.""SalaryMax"" >= {{{paramIdx}}}";
            parameters.Add(request.MinSalary.Value);
            paramIdx++;
        }

        if (request.VisaSponsorship.HasValue)
        {
            sql += $@" AND j.""VisaSponsorship"" = {{{paramIdx}}}";
            parameters.Add(request.VisaSponsorship.Value);
            paramIdx++;
        }

        if (request.Remote.HasValue)
        {
            sql += $@" AND j.""Remote"" = {{{paramIdx}}}";
            parameters.Add(request.Remote.Value);
            paramIdx++;
        }

        sql += @"
            ORDER BY RelevanceScore DESC
            LIMIT {" + paramIdx + "} OFFSET {" + (paramIdx + 1) + "}";
        parameters.Add(request.TopN);
        parameters.Add(request.Offset);

        // Execute and map results
        var results = new List<JobSearchResult>();

        using var command = _db.Database.GetDbConnection().CreateCommand();
        command.CommandText = string.Format(sql, parameters.Select((_, i) => $"@p{i}").ToArray());

        for (var i = 0; i < parameters.Count; i++)
        {
            var param = command.CreateParameter();
            param.ParameterName = $"@p{i}";
            param.Value = parameters[i];
            command.Parameters.Add(param);
        }

        await _db.Database.OpenConnectionAsync(ct);

        try
        {
            using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                results.Add(new JobSearchResult
                {
                    JobId = reader.GetGuid(reader.GetOrdinal("JobId")),
                    Title = reader.GetString(reader.GetOrdinal("Title")),
                    Company = reader.IsDBNull(reader.GetOrdinal("Company")) ? null : reader.GetString(reader.GetOrdinal("Company")),
                    Location = reader.IsDBNull(reader.GetOrdinal("Location")) ? null : reader.GetString(reader.GetOrdinal("Location")),
                    CountryCode = reader.GetString(reader.GetOrdinal("CountryCode")),
                    Industry = reader.IsDBNull(reader.GetOrdinal("Industry")) ? null : reader.GetString(reader.GetOrdinal("Industry")),
                    Source = reader.IsDBNull(reader.GetOrdinal("Source")) ? null : reader.GetString(reader.GetOrdinal("Source")),
                    Url = reader.GetString(reader.GetOrdinal("Url")),
                    SalaryMin = reader.IsDBNull(reader.GetOrdinal("SalaryMin")) ? null : reader.GetInt32(reader.GetOrdinal("SalaryMin")),
                    SalaryMax = reader.IsDBNull(reader.GetOrdinal("SalaryMax")) ? null : reader.GetInt32(reader.GetOrdinal("SalaryMax")),
                    Currency = reader.IsDBNull(reader.GetOrdinal("Currency")) ? null : reader.GetString(reader.GetOrdinal("Currency")),
                    Remote = reader.IsDBNull(reader.GetOrdinal("Remote")) ? null : reader.GetBoolean(reader.GetOrdinal("Remote")),
                    VisaSponsorship = reader.IsDBNull(reader.GetOrdinal("VisaSponsorship")) ? null : reader.GetBoolean(reader.GetOrdinal("VisaSponsorship")),
                    LanguageRequired = reader.IsDBNull(reader.GetOrdinal("LanguageRequired")) ? null : reader.GetString(reader.GetOrdinal("LanguageRequired")),
                    PostedDate = reader.IsDBNull(reader.GetOrdinal("PostedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("PostedDate")),
                    DescriptionSnippet = reader.IsDBNull(reader.GetOrdinal("DescriptionSnippet")) ? null : reader.GetString(reader.GetOrdinal("DescriptionSnippet")),
                    TextScore = reader.GetFloat(reader.GetOrdinal("TextScore")),
                    VectorScore = reader.GetFloat(reader.GetOrdinal("VectorScore")),
                    RelevanceScore = reader.GetFloat(reader.GetOrdinal("RelevanceScore"))
                });
            }
        }
        finally
        {
            await _db.Database.CloseConnectionAsync();
        }

        _logger.LogInformation("Hybrid search returned {Count} results", results.Count);
        return results;
    }
}
