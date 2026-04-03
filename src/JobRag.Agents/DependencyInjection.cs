using JobRag.Agents.Agents;
using JobRag.Agents.Guardrails;
using JobRag.Agents.Workflows;
using Microsoft.Extensions.DependencyInjection;

namespace JobRag.Agents;

/// <summary>
/// Registers all AI agents, guardrails, and workflows with the DI container.
/// Call services.AddAgents() from your host project.
///
/// Prerequisites — register an IChatClient before calling this:
///   builder.Services.AddOpenAIClient(...).AddChatClient("gpt-4o-mini");
///   // or Azure OpenAI, Ollama, etc. — any Microsoft.Extensions.AI provider.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddAgents(this IServiceCollection services)
    {
        // Agents
        services.AddScoped<QueryRewriteAgent>();
        services.AddScoped<CoverLetterAgent>();
        services.AddScoped<InterviewPrepAgent>();
        services.AddScoped<JobMatchAgent>();
        services.AddScoped<JobSummaryAgent>();

        // Guardrails
        services.AddSingleton<PiiGuardrail>();
        services.AddSingleton<HallucinationGuardrail>();
        services.AddSingleton(new ContentLengthGuardrail(minWords: 100, maxWords: 500));
        services.AddSingleton(new CostBudgetGuardrail(maxTokensPerRequest: 4000, maxRequestsPerHour: 50));

        // Workflows
        services.AddScoped<JobApplicationWorkflow>();
        services.AddScoped<SmartSearchWorkflow>();

        return services;
    }
}
