using JobRag.Agents.Abstractions;

namespace JobRag.Agents.Guardrails;

/// <summary>
/// Validates that generated content stays within acceptable length bounds.
/// Prevents runaway generation costs and ensures output quality.
///
/// Applied to: Cover letters (250-400 words), summaries (50-200 words).
/// </summary>
public class ContentLengthGuardrail : IGuardrail<string>
{
    private readonly int _minWords;
    private readonly int _maxWords;

    public string Name => $"ContentLength ({_minWords}-{_maxWords} words)";

    public ContentLengthGuardrail(int minWords = 50, int maxWords = 500)
    {
        _minWords = minWords;
        _maxWords = maxWords;
    }

    public Task<GuardrailResult<string>> ValidateAsync(string content, CancellationToken ct = default)
    {
        var wordCount = content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

        if (wordCount < _minWords)
        {
            return Task.FromResult(
                GuardrailResult<string>.Block(
                    $"Content too short: {wordCount} words (minimum {_minWords})"));
        }

        if (wordCount > _maxWords)
        {
            return Task.FromResult(
                GuardrailResult<string>.Warn(content,
                    $"Content may be too long: {wordCount} words (recommended max {_maxWords})"));
        }

        return Task.FromResult(GuardrailResult<string>.Pass(content));
    }
}
