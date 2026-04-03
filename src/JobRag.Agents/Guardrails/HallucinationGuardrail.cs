using JobRag.Agents.Abstractions;

namespace JobRag.Agents.Guardrails;

/// <summary>
/// Validates that generated cover letters don't fabricate qualifications
/// the candidate doesn't have. Cross-references claims against the original CV text.
///
/// Simple heuristic approach: checks that key technical terms in the cover letter
/// also appear in the CV. A more advanced version could use embeddings.
/// </summary>
public class HallucinationGuardrail : IGuardrail<HallucinationCheckInput>
{
    public string Name => "Hallucination Check";

    /// <summary>
    /// Common tech keywords to cross-reference between CV and generated content.
    /// If the generated text mentions a technology not in the CV, flag it.
    /// </summary>
    private static readonly HashSet<string> TechKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "Python", "Java", "C#", "JavaScript", "TypeScript", "Go", "Rust", "Kotlin", "Swift",
        "React", "Angular", "Vue", "Next.js", "Node.js", "Django", "Flask", "Spring",
        "AWS", "Azure", "GCP", "Docker", "Kubernetes", "Terraform",
        "PostgreSQL", "MongoDB", "Redis", "Elasticsearch", "Kafka",
        "PhD", "Master", "Bachelor", "MBA", "CPA", "PMP", "Scrum Master",
        "Machine Learning", "Deep Learning", "NLP", "Computer Vision",
        "TensorFlow", "PyTorch", "Spark", "Hadoop"
    };

    public Task<GuardrailResult<HallucinationCheckInput>> ValidateAsync(
        HallucinationCheckInput input, CancellationToken ct = default)
    {
        var fabricatedClaims = new List<string>();

        foreach (var keyword in TechKeywords)
        {
            var inGenerated = input.GeneratedText.Contains(keyword, StringComparison.OrdinalIgnoreCase);
            var inCv = input.OriginalCvText.Contains(keyword, StringComparison.OrdinalIgnoreCase);

            // If the generated text claims a skill not in the CV, flag it
            if (inGenerated && !inCv)
            {
                fabricatedClaims.Add(keyword);
            }
        }

        if (fabricatedClaims.Count > 2)
        {
            return Task.FromResult(
                GuardrailResult<HallucinationCheckInput>.Block(
                    $"Possible fabricated qualifications: {string.Join(", ", fabricatedClaims)}"));
        }

        if (fabricatedClaims.Count > 0)
        {
            return Task.FromResult(
                GuardrailResult<HallucinationCheckInput>.Warn(input,
                    $"Minor concern — mentions not in CV: {string.Join(", ", fabricatedClaims)}"));
        }

        return Task.FromResult(GuardrailResult<HallucinationCheckInput>.Pass(input));
    }
}

public record HallucinationCheckInput
{
    public string GeneratedText { get; init; } = default!;
    public string OriginalCvText { get; init; } = default!;
}
