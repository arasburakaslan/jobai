using JobRag.Agents.Abstractions;
using JobRag.Agents.Agents;
using Microsoft.Extensions.Logging;

namespace JobRag.Agents.Workflows;

/// <summary>
/// Intelligent search workflow:
/// 1. QueryRewrite agent → parse natural language into structured filters
/// 2. Execute hybrid search (delegated to IHybridSearchService via caller)
/// 3. JobSummary agent → summarise top results for quick scanning
///
/// This workflow is called by the search endpoint to provide
/// conversational-style job search ("find me remote C# jobs in Berlin").
/// </summary>
public class SmartSearchWorkflow : IWorkflow<SmartSearchInput, SmartSearchOutput>
{
    private readonly QueryRewriteAgent _queryRewriteAgent;
    private readonly JobSummaryAgent _summaryAgent;
    private readonly ILogger<SmartSearchWorkflow> _logger;

    public string Name => "SmartSearch";
    public string Description => "Converts natural language to structured search, then summarises results.";

    public SmartSearchWorkflow(
        QueryRewriteAgent queryRewriteAgent,
        JobSummaryAgent summaryAgent,
        ILogger<SmartSearchWorkflow> logger)
    {
        _queryRewriteAgent = queryRewriteAgent;
        _summaryAgent = summaryAgent;
        _logger = logger;
    }

    public async Task<WorkflowResult<SmartSearchOutput>> RunAsync(
        SmartSearchInput input, CancellationToken ct = default)
    {
        var totalSw = System.Diagnostics.Stopwatch.StartNew();
        var steps = new List<WorkflowStep>();

        // Step 1: Rewrite the query
        _logger.LogInformation("SmartSearch Step 1: Rewriting query");
        var rewriteResult = await _queryRewriteAgent.ExecuteAsync(
            new QueryRewriteInput { RawQuery = input.NaturalLanguageQuery }, ct);

        steps.Add(new WorkflowStep
        {
            AgentName = _queryRewriteAgent.Name,
            Success = rewriteResult.Success,
            Duration = rewriteResult.Duration,
            Error = rewriteResult.Error
        });

        if (!rewriteResult.Success)
        {
            totalSw.Stop();
            return new WorkflowResult<SmartSearchOutput>
            {
                Success = false,
                Error = $"Query rewrite failed: {rewriteResult.Error}",
                Steps = steps,
                TotalDuration = totalSw.Elapsed
            };
        }

        // Output the rewritten query — the caller uses this to run the actual search.
        // Optionally, step 2 (summarisation) can be triggered after search results come back.
        totalSw.Stop();
        return new WorkflowResult<SmartSearchOutput>
        {
            Success = true,
            Data = new SmartSearchOutput
            {
                RewrittenQuery = rewriteResult.Data!
            },
            Steps = steps,
            TotalDuration = totalSw.Elapsed
        };
    }
}

public record SmartSearchInput
{
    public string NaturalLanguageQuery { get; init; } = default!;
}

public record SmartSearchOutput
{
    public QueryRewriteOutput RewrittenQuery { get; init; } = default!;
    public List<JobSummaryOutput> Summaries { get; init; } = [];
}
