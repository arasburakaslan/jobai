using JobRag.Agents.Abstractions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace JobRag.Agents.Agents;

/// <summary>
/// Computes a match score between a user's CV/profile and a job description.
/// Uses the LLM to perform semantic analysis beyond simple vector similarity,
/// evaluating skill overlap, experience level match, and cultural fit signals.
///
/// Returns a structured breakdown: overall score + per-dimension scores + explanation.
/// </summary>
public class JobMatchAgent : IAgent<JobMatchInput, JobMatchOutput>
{
    private readonly IChatClient _chatClient;
    private readonly ILogger<JobMatchAgent> _logger;

    public string Name => "JobMatch";
    public string Description => "Scores how well a candidate matches a job based on CV analysis.";

    public JobMatchAgent(IChatClient chatClient, ILogger<JobMatchAgent> logger)
    {
        _chatClient = chatClient;
        _logger = logger;
    }

    public async Task<AgentResult<JobMatchOutput>> ExecuteAsync(
        JobMatchInput input, CancellationToken ct = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, """
                    You are an expert recruiter. Score the candidate-job match on a 0-100 scale.
                    Provide scores for each dimension and a brief explanation.

                    Respond in JSON:
                    {
                      "overallScore": 0-100,
                      "skillMatch": 0-100,
                      "experienceMatch": 0-100,
                      "locationMatch": 0-100,
                      "salaryMatch": 0-100,
                      "strengths": ["strength1", "strength2"],
                      "gaps": ["gap1", "gap2"],
                      "recommendation": "Strong match / Moderate match / Weak match",
                      "explanation": "Brief 2-3 sentence summary"
                    }

                    Be honest. If the candidate lacks key requirements, reflect that in the score.
                    """),
                new(ChatRole.User, $"""
                    ## Job Description
                    {input.JobDescription}

                    ## Candidate CV
                    {input.CvText}

                    ## Additional Candidate Preferences
                    Preferred location: {input.PreferredLocation ?? "Any"}
                    Minimum salary: {input.MinSalary?.ToString() ?? "Not specified"}
                    """)
            };

            var options = new ChatOptions
            {
                Temperature = 0.1f,
                MaxOutputTokens = 800,
                ModelId = "gpt-4o-mini"
            };

            var completion = await _chatClient.GetResponseAsync(messages, options, ct);

            var output = new JobMatchOutput
            {
                JobId = input.JobId,
                RawLlmResponse = completion.Text
                // TODO: Deserialize into structured scores
            };

            sw.Stop();
            return AgentResult<JobMatchOutput>.Ok(output, sw.Elapsed, (int)(completion.Usage?.TotalTokenCount ?? 0));
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "JobMatch agent failed");
            return AgentResult<JobMatchOutput>.Fail(ex.Message, sw.Elapsed);
        }
    }
}

public record JobMatchInput
{
    public Guid? JobId { get; init; }
    public string JobDescription { get; init; } = default!;
    public string CvText { get; init; } = default!;
    public string? PreferredLocation { get; init; }
    public int? MinSalary { get; init; }
}

public record JobMatchOutput
{
    public Guid? JobId { get; init; }
    public int OverallScore { get; init; }
    public int SkillMatch { get; init; }
    public int ExperienceMatch { get; init; }
    public int LocationMatch { get; init; }
    public int SalaryMatch { get; init; }
    public List<string> Strengths { get; init; } = [];
    public List<string> Gaps { get; init; } = [];
    public string? Recommendation { get; init; }
    public string? Explanation { get; init; }
    public string? RawLlmResponse { get; init; }
}
