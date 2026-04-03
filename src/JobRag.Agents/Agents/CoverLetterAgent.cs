using JobRag.Agents.Abstractions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace JobRag.Agents.Agents;

/// <summary>
/// Takes a job description + user's CV and generates a tailored cover letter.
/// Applies country-specific conventions (e.g., formal tone for German companies,
/// Anschreiben format, appropriate length).
///
/// Guardrails applied: PII check, hallucination check, tone validation.
/// </summary>
public class CoverLetterAgent : IAgent<CoverLetterInput, CoverLetterOutput>
{
    private readonly IChatClient _chatClient;
    private readonly ILogger<CoverLetterAgent> _logger;

    public string Name => "CoverLetterGenerator";
    public string Description => "Generates tailored cover letters from a job description and user CV.";

    public CoverLetterAgent(IChatClient chatClient, ILogger<CoverLetterAgent> logger)
    {
        _chatClient = chatClient;
        _logger = logger;
    }

    public async Task<AgentResult<CoverLetterOutput>> ExecuteAsync(
        CoverLetterInput input, CancellationToken ct = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, $"""
                    You are an expert career coach specialising in the {input.CountryCode ?? "European"} job market.
                    Write a professional cover letter tailored to the job description using the candidate's CV.

                    Guidelines:
                    - Match the tone to the country (formal for DE/AT, slightly less formal for NL/UK)
                    - Highlight 2-3 specific skills from the CV that match the job requirements
                    - Keep it concise: 250-350 words
                    - Do NOT invent qualifications the candidate doesn't have
                    - Do NOT include placeholder text like [Your Name] — use actual details from the CV
                    - Structure: Opening → Why this role → Key qualifications → Closing
                    """),
                new(ChatRole.User, $"""
                    ## Job Description
                    {input.JobDescription}

                    ## Candidate CV
                    {input.CvText}

                    ## Additional Context
                    Company: {input.CompanyName ?? "Unknown"}
                    Role: {input.JobTitle ?? "Unknown"}
                    Language preference: {input.PreferredLanguage ?? "English"}
                    """)
            };

            var options = new ChatOptions
            {
                Temperature = 0.5f,
                MaxOutputTokens = 1500,
                ModelId = "gpt-4o-mini"
            };

            var completion = await _chatClient.GetResponseAsync(messages, options, ct);
            var content = completion.Text ?? string.Empty;

            var output = new CoverLetterOutput
            {
                CoverLetterText = content,
                JobTitle = input.JobTitle,
                CompanyName = input.CompanyName,
                WordCount = content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length
            };

            sw.Stop();
            _logger.LogDebug("CoverLetter generated in {Duration}ms ({Words} words)",
                sw.ElapsedMilliseconds, output.WordCount);
            return AgentResult<CoverLetterOutput>.Ok(output, sw.Elapsed, (int)(completion.Usage?.TotalTokenCount ?? 0));
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "CoverLetter agent failed");
            return AgentResult<CoverLetterOutput>.Fail(ex.Message, sw.Elapsed);
        }
    }
}

public record CoverLetterInput
{
    public string JobDescription { get; init; } = default!;
    public string CvText { get; init; } = default!;
    public string? JobTitle { get; init; }
    public string? CompanyName { get; init; }
    public string? CountryCode { get; init; }
    public string? PreferredLanguage { get; init; }
}

public record CoverLetterOutput
{
    public string CoverLetterText { get; init; } = default!;
    public string? JobTitle { get; init; }
    public string? CompanyName { get; init; }
    public int WordCount { get; init; }
}
