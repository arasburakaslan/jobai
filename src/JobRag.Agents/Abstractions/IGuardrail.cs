namespace JobRag.Agents.Abstractions;

/// <summary>
/// A guardrail validates or transforms agent input/output to enforce safety,
/// quality, and business rules. Guardrails can block, modify, or flag content.
/// </summary>
public interface IGuardrail<T>
{
    string Name { get; }

    /// <summary>
    /// Validate the content. Returns a result indicating pass/fail with optional modified content.
    /// </summary>
    Task<GuardrailResult<T>> ValidateAsync(T content, CancellationToken ct = default);
}

public record GuardrailResult<T>
{
    public bool Passed { get; init; }
    public T? Content { get; init; }
    public string? Reason { get; init; }
    public GuardrailSeverity Severity { get; init; }

    public static GuardrailResult<T> Pass(T content) =>
        new() { Passed = true, Content = content, Severity = GuardrailSeverity.None };

    public static GuardrailResult<T> Warn(T content, string reason) =>
        new() { Passed = true, Content = content, Reason = reason, Severity = GuardrailSeverity.Warning };

    public static GuardrailResult<T> Block(string reason) =>
        new() { Passed = false, Reason = reason, Severity = GuardrailSeverity.Blocked };
}

public enum GuardrailSeverity
{
    None,
    Warning,
    Blocked
}
