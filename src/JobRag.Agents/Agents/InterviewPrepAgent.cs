using JobRag.Agents.Abstractions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace JobRag.Agents.Agents;

/// <summary>
/// Analyses a job description and generates likely interview questions,
/// categorised by type (behavioural, technical, situational).
/// Optionally tailored to the candidate's CV for personalised prep.
/// </summary>
public class InterviewPrepAgent : IAgent<InterviewPrepInput, InterviewPrepOutput>
{
    private readonly IChatClient _chatClient;
    private readonly ILogger<InterviewPrepAgent> _logger;

    public string Name => "InterviewPrep";
    public string Description => "Generates likely interview questions from a job description.";

    public InterviewPrepAgent(IChatClient chatClient, ILogger<InterviewPrepAgent> logger)
    {
        _chatClient = chatClient;
        _logger = logger;
    }

    public async Task<AgentResult<InterviewPrepOutput>> ExecuteAsync(
        InterviewPrepInput input, CancellationToken ct = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var cvContext = string.IsNullOrWhiteSpace(input.CvText)
                ? ""
                : $"\n\n## Candidate CV (use to personalise questions)\n{input.CvText}";

            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, """
                    You are a senior technical recruiter and interview coach.
                    Given a job description, generate interview questions in JSON format:
                    {
                      "technical": ["question1", "question2", ...],
                      "behavioural": ["question1", "question2", ...],
                      "situational": ["question1", "question2", ...],
                      "companySpecific": ["question1", ...]
                    }

                    Generate 3-5 questions per category.
                    Make questions specific to the technologies and responsibilities mentioned.
                    If a CV is provided, include questions about gaps or transitions.
                    """),
                new(ChatRole.User, $"""
                    ## Job Description
                    {input.JobDescription}
                    {cvContext}
                    """)
            };

            var options = new ChatOptions
            {
                Temperature = 0.4f,
                MaxOutputTokens = 2000,
                ModelId = "gpt-4o-mini"
            };

            var completion = await _chatClient.GetResponseAsync(messages, options, ct);

            var output = new InterviewPrepOutput
            {
                JobTitle = input.JobTitle,
                RawLlmResponse = completion.Text
                // TODO: Deserialize into structured question categories
            };

            sw.Stop();
            return AgentResult<InterviewPrepOutput>.Ok(output, sw.Elapsed, (int)(completion.Usage?.TotalTokenCount ?? 0));
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "InterviewPrep agent failed");
            return AgentResult<InterviewPrepOutput>.Fail(ex.Message, sw.Elapsed);
        }
    }
}

public record InterviewPrepInput
{
    public string JobDescription { get; init; } = default!;
    public string? JobTitle { get; init; }
    public string? CvText { get; init; }
}

public record InterviewPrepOutput
{
    public string? JobTitle { get; init; }
    public List<string> TechnicalQuestions { get; init; } = [];
    public List<string> BehaviouralQuestions { get; init; } = [];
    public List<string> SituationalQuestions { get; init; } = [];
    public List<string> CompanySpecificQuestions { get; init; } = [];
    public string? RawLlmResponse { get; init; }
}
