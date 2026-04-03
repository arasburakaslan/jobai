using JobRag.Agents.Abstractions;

namespace JobRag.Agents.Guardrails;

/// <summary>
/// Enforces rate limiting and cost budgets on LLM calls per user/session.
/// Prevents abuse and runaway API costs.
///
/// Tracks token usage and blocks requests that exceed the configured budget.
/// </summary>
public class CostBudgetGuardrail : IGuardrail<CostBudgetInput>
{
    private readonly int _maxTokensPerRequest;
    private readonly int _maxRequestsPerHour;

    public string Name => "Cost Budget";

    public CostBudgetGuardrail(int maxTokensPerRequest = 4000, int maxRequestsPerHour = 50)
    {
        _maxTokensPerRequest = maxTokensPerRequest;
        _maxRequestsPerHour = maxRequestsPerHour;
    }

    public Task<GuardrailResult<CostBudgetInput>> ValidateAsync(
        CostBudgetInput input, CancellationToken ct = default)
    {
        if (input.EstimatedTokens > _maxTokensPerRequest)
        {
            return Task.FromResult(
                GuardrailResult<CostBudgetInput>.Block(
                    $"Request would use ~{input.EstimatedTokens} tokens (limit: {_maxTokensPerRequest})"));
        }

        if (input.RequestsThisHour >= _maxRequestsPerHour)
        {
            return Task.FromResult(
                GuardrailResult<CostBudgetInput>.Block(
                    $"Hourly request limit reached ({_maxRequestsPerHour}/hour)"));
        }

        if (input.RequestsThisHour > _maxRequestsPerHour * 0.8)
        {
            return Task.FromResult(
                GuardrailResult<CostBudgetInput>.Warn(input,
                    $"Approaching hourly limit: {input.RequestsThisHour}/{_maxRequestsPerHour}"));
        }

        return Task.FromResult(GuardrailResult<CostBudgetInput>.Pass(input));
    }
}

public record CostBudgetInput
{
    public int EstimatedTokens { get; init; }
    public int RequestsThisHour { get; init; }
    public string? UserId { get; init; }
}
