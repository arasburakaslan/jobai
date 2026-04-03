using JobRag.Agents.Abstractions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace JobRag.Agents.Agents;

/// <summary>
/// Summarises a long job description into a concise, structured format.
/// Useful for displaying job cards, notifications, and comparison views.
/// Extracts: key requirements, nice-to-haves, red flags, and a TL;DR.
/// </summary>
public class JobSummaryAgent : IAgent<JobSummaryInput, JobSummaryOutput>
{
    private readonly IChatClient _chatClient;
    private readonly ILogger<JobSummaryAgent> _logger;

    public string Name => "JobSummary";
    public string Description => "Summarises job descriptions into structured, concise formats.";

    public JobSummaryAgent(IChatClient chatClient, ILogger<JobSummaryAgent> logger)
    {
        _chatClient = chatClient;
        _logger = logger;
    }

    public async Task<AgentResult<JobSummaryOutput>> ExecuteAsync(
        JobSummaryInput input, CancellationToken ct = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, """
                    You are a job listing analyst. Summarise the job description into this JSON structure:
                    {
                      "tldr": "One sentence summary of the role",
                      "mustHave": ["requirement1", "requirement2"],
                      "niceToHave": ["skill1", "skill2"],
                      "techStack": ["technology1", "technology2"],
                      "redFlags": ["any concerning patterns like unpaid overtime, unrealistic expectations"],
                      "seniorityLevel": "Junior / Mid / Senior / Lead / Principal",
                      "estimatedYearsExperience": "2-4"
                    }

                    Be objective. If the posting has red flags (vague salary, excessive requirements for
                    the level, "we're a family" language), note them honestly.
                    """),
                new(ChatRole.User, input.JobDescription)
            };

            var options = new ChatOptions
            {
                Temperature = 0.2f,
                MaxOutputTokens = 800,
                ModelId = "gpt-4o-mini"
            };

            var completion = await _chatClient.GetResponseAsync(messages, options, ct);

            var output = new JobSummaryOutput
            {
                JobId = input.JobId,
                RawLlmResponse = completion.Text
            };

            sw.Stop();
            return AgentResult<JobSummaryOutput>.Ok(output, sw.Elapsed, (int)(completion.Usage?.TotalTokenCount ?? 0));
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "JobSummary agent failed");
            return AgentResult<JobSummaryOutput>.Fail(ex.Message, sw.Elapsed);
        }
    }
}

public record JobSummaryInput
{
    public Guid? JobId { get; init; }
    public string JobDescription { get; init; } = default!;
}

public record JobSummaryOutput
{
    public Guid? JobId { get; init; }
    public string? Tldr { get; init; }
    public List<string> MustHave { get; init; } = [];
    public List<string> NiceToHave { get; init; } = [];
    public List<string> TechStack { get; init; } = [];
    public List<string> RedFlags { get; init; } = [];
    public string? SeniorityLevel { get; init; }
    public string? EstimatedYearsExperience { get; init; }
    public string? RawLlmResponse { get; init; }
}
