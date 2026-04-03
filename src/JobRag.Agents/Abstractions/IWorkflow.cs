namespace JobRag.Agents.Abstractions;

/// <summary>
/// A workflow orchestrates multiple agents in sequence or parallel,
/// applying guardrails at each step, to complete a complex task.
/// </summary>
public interface IWorkflow<TInput, TOutput>
{
    string Name { get; }
    string Description { get; }

    Task<WorkflowResult<TOutput>> RunAsync(TInput input, CancellationToken ct = default);
}

public record WorkflowResult<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Error { get; init; }
    public List<WorkflowStep> Steps { get; init; } = [];
    public TimeSpan TotalDuration { get; init; }
}

public record WorkflowStep
{
    public string AgentName { get; init; } = default!;
    public bool Success { get; init; }
    public TimeSpan Duration { get; init; }
    public string? Error { get; init; }
}
