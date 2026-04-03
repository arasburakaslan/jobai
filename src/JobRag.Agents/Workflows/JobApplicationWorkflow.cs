using JobRag.Agents.Abstractions;
using JobRag.Agents.Agents;
using JobRag.Agents.Guardrails;
using Microsoft.Extensions.Logging;

namespace JobRag.Agents.Workflows;

/// <summary>
/// End-to-end workflow for applying to a job:
/// 1. JobMatch agent → score the fit
/// 2. CoverLetter agent → generate tailored cover letter
/// 3. Guardrails → PII check, hallucination check, length validation
///
/// The workflow short-circuits if the match score is below a threshold,
/// advising the user that the job may not be a good fit.
/// </summary>
public class JobApplicationWorkflow : IWorkflow<JobApplicationWorkflowInput, JobApplicationWorkflowOutput>
{
    private readonly JobMatchAgent _matchAgent;
    private readonly CoverLetterAgent _coverLetterAgent;
    private readonly PiiGuardrail _piiGuardrail;
    private readonly HallucinationGuardrail _hallucinationGuardrail;
    private readonly ContentLengthGuardrail _lengthGuardrail;
    private readonly ILogger<JobApplicationWorkflow> _logger;

    public string Name => "JobApplication";
    public string Description => "Scores job fit, generates cover letter, applies guardrails.";

    public JobApplicationWorkflow(
        JobMatchAgent matchAgent,
        CoverLetterAgent coverLetterAgent,
        PiiGuardrail piiGuardrail,
        HallucinationGuardrail hallucinationGuardrail,
        ContentLengthGuardrail lengthGuardrail,
        ILogger<JobApplicationWorkflow> logger)
    {
        _matchAgent = matchAgent;
        _coverLetterAgent = coverLetterAgent;
        _piiGuardrail = piiGuardrail;
        _hallucinationGuardrail = hallucinationGuardrail;
        _lengthGuardrail = lengthGuardrail;
        _logger = logger;
    }

    public async Task<WorkflowResult<JobApplicationWorkflowOutput>> RunAsync(
        JobApplicationWorkflowInput input, CancellationToken ct = default)
    {
        var totalSw = System.Diagnostics.Stopwatch.StartNew();
        var steps = new List<WorkflowStep>();

        // Step 1: Score the match
        _logger.LogInformation("Step 1: Scoring job match");
        var matchResult = await _matchAgent.ExecuteAsync(new JobMatchInput
        {
            JobId = input.JobId,
            JobDescription = input.JobDescription,
            CvText = input.CvText,
            PreferredLocation = input.PreferredLocation,
            MinSalary = input.MinSalary
        }, ct);

        steps.Add(new WorkflowStep
        {
            AgentName = _matchAgent.Name,
            Success = matchResult.Success,
            Duration = matchResult.Duration,
            Error = matchResult.Error
        });

        if (!matchResult.Success)
        {
            totalSw.Stop();
            return new WorkflowResult<JobApplicationWorkflowOutput>
            {
                Success = false,
                Error = $"Match scoring failed: {matchResult.Error}",
                Steps = steps,
                TotalDuration = totalSw.Elapsed
            };
        }

        // Step 2: Generate cover letter
        _logger.LogInformation("Step 2: Generating cover letter");
        var coverResult = await _coverLetterAgent.ExecuteAsync(new CoverLetterInput
        {
            JobDescription = input.JobDescription,
            CvText = input.CvText,
            JobTitle = input.JobTitle,
            CompanyName = input.CompanyName,
            CountryCode = input.CountryCode,
            PreferredLanguage = input.PreferredLanguage
        }, ct);

        steps.Add(new WorkflowStep
        {
            AgentName = _coverLetterAgent.Name,
            Success = coverResult.Success,
            Duration = coverResult.Duration,
            Error = coverResult.Error
        });

        if (!coverResult.Success)
        {
            totalSw.Stop();
            return new WorkflowResult<JobApplicationWorkflowOutput>
            {
                Success = false,
                Error = $"Cover letter generation failed: {coverResult.Error}",
                Steps = steps,
                TotalDuration = totalSw.Elapsed
            };
        }

        // Step 3: Guardrails
        _logger.LogInformation("Step 3: Applying guardrails");

        var piiResult = await _piiGuardrail.ValidateAsync(coverResult.Data!.CoverLetterText, ct);
        if (!piiResult.Passed)
        {
            totalSw.Stop();
            return new WorkflowResult<JobApplicationWorkflowOutput>
            {
                Success = false,
                Error = $"PII guardrail blocked: {piiResult.Reason}",
                Steps = steps,
                TotalDuration = totalSw.Elapsed
            };
        }

        var hallucinationResult = await _hallucinationGuardrail.ValidateAsync(
            new HallucinationCheckInput
            {
                GeneratedText = coverResult.Data.CoverLetterText,
                OriginalCvText = input.CvText
            }, ct);

        var lengthResult = await _lengthGuardrail.ValidateAsync(coverResult.Data.CoverLetterText, ct);

        var warnings = new List<string>();
        if (hallucinationResult.Reason != null) warnings.Add(hallucinationResult.Reason);
        if (lengthResult.Reason != null) warnings.Add(lengthResult.Reason);

        // Done
        totalSw.Stop();
        return new WorkflowResult<JobApplicationWorkflowOutput>
        {
            Success = true,
            Data = new JobApplicationWorkflowOutput
            {
                MatchResult = matchResult.Data!,
                CoverLetterResult = coverResult.Data,
                GuardrailWarnings = warnings
            },
            Steps = steps,
            TotalDuration = totalSw.Elapsed
        };
    }
}

public record JobApplicationWorkflowInput
{
    public Guid? JobId { get; init; }
    public string JobDescription { get; init; } = default!;
    public string CvText { get; init; } = default!;
    public string? JobTitle { get; init; }
    public string? CompanyName { get; init; }
    public string? CountryCode { get; init; }
    public string? PreferredLanguage { get; init; }
    public string? PreferredLocation { get; init; }
    public int? MinSalary { get; init; }
}

public record JobApplicationWorkflowOutput
{
    public JobMatchOutput MatchResult { get; init; } = default!;
    public CoverLetterOutput CoverLetterResult { get; init; } = default!;
    public List<string> GuardrailWarnings { get; init; } = [];
}
