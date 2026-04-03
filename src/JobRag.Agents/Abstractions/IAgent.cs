namespace JobRag.Agents.Abstractions;

/// <summary>
/// Base interface for all AI agents in the platform.
/// Each agent has a single responsibility and can be composed into workflows.
/// </summary>
public interface IAgent<TInput, TOutput>
{
    string Name { get; }
    string Description { get; }

    /// <summary>
    /// Execute the agent's core logic.
    /// </summary>
    Task<AgentResult<TOutput>> ExecuteAsync(TInput input, CancellationToken ct = default);
}

/// <summary>
/// Wraps the result of an agent execution with success/failure metadata.
/// </summary>
public record AgentResult<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Error { get; init; }
    public TimeSpan Duration { get; init; }
    public int TokensUsed { get; init; }

    public static AgentResult<T> Ok(T data, TimeSpan duration, int tokens = 0) =>
        new() { Success = true, Data = data, Duration = duration, TokensUsed = tokens };

    public static AgentResult<T> Fail(string error, TimeSpan duration) =>
        new() { Success = false, Error = error, Duration = duration };
}
