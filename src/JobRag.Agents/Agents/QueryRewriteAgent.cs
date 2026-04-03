using JobRag.Agents.Abstractions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace JobRag.Agents.Agents;

/// <summary>
/// Rewrites a user's natural language job search query into an optimised search
/// query with extracted structured filters (country, remote, salary, etc.).
///
/// Example:
///   Input:  "I want a remote C# job in Berlin paying at least 70k with visa sponsorship"
///   Output: { OptimisedQuery: "C# .NET developer", Filters: { Country: "DE", City: "Berlin", Remote: true, ... } }
/// </summary>
public class QueryRewriteAgent : IAgent<QueryRewriteInput, QueryRewriteOutput>
{
    private readonly IChatClient _chatClient;
    private readonly ILogger<QueryRewriteAgent> _logger;

    public string Name => "QueryRewrite";
    public string Description => "Rewrites natural language job queries into optimised search queries with structured filters.";

    public QueryRewriteAgent(IChatClient chatClient, ILogger<QueryRewriteAgent> logger)
    {
        _chatClient = chatClient;
        _logger = logger;
    }

    public async Task<AgentResult<QueryRewriteOutput>> ExecuteAsync(
        QueryRewriteInput input, CancellationToken ct = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, """
                    You are a job search query optimizer. Given a user's natural language query,
                    extract:
                    1. An optimised keyword search query (remove filler words, focus on skills/roles)
                    2. Structured filters: countryCode, city, remote (bool), minSalary (int),
                       visaSponsorship (bool), industry, languageRequired

                    Respond in JSON format matching this schema:
                    {
                      "optimisedQuery": "string",
                      "countryCode": "string or null",
                      "city": "string or null",
                      "remote": "bool or null",
                      "minSalary": "int or null",
                      "visaSponsorship": "bool or null",
                      "industry": "string or null",
                      "languageRequired": "string or null"
                    }
                    """),
                new(ChatRole.User, input.RawQuery)
            };

            var options = new ChatOptions
            {
                Temperature = 0.1f,
                MaxOutputTokens = 500,
                ModelId = "gpt-4o-mini"
            };

            var completion = await _chatClient.GetResponseAsync(messages, options, ct);

            // TODO: Deserialize completion.Message.Text into QueryRewriteOutput
            // For now, return a placeholder showing the structure
            var output = new QueryRewriteOutput
            {
                OriginalQuery = input.RawQuery,
                OptimisedQuery = input.RawQuery, // Replace with parsed result
                RawLlmResponse = completion.Text
            };

            sw.Stop();
            _logger.LogDebug("QueryRewrite completed in {Duration}ms", sw.ElapsedMilliseconds);
            return AgentResult<QueryRewriteOutput>.Ok(output, sw.Elapsed, (int)(completion.Usage?.TotalTokenCount ?? 0));
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "QueryRewrite agent failed");
            return AgentResult<QueryRewriteOutput>.Fail(ex.Message, sw.Elapsed);
        }
    }
}

public record QueryRewriteInput
{
    public string RawQuery { get; init; } = default!;
}

public record QueryRewriteOutput
{
    public string OriginalQuery { get; init; } = default!;
    public string OptimisedQuery { get; init; } = default!;
    public string? CountryCode { get; init; }
    public string? City { get; init; }
    public bool? Remote { get; init; }
    public int? MinSalary { get; init; }
    public bool? VisaSponsorship { get; init; }
    public string? Industry { get; init; }
    public string? LanguageRequired { get; init; }
    public string? RawLlmResponse { get; init; }
}
