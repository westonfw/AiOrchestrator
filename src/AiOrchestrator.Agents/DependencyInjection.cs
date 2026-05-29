using AiOrchestrator.Application;
using Microsoft.Extensions.DependencyInjection;

namespace AiOrchestrator.Agents;

public static class DependencyInjection
{
    public static IServiceCollection AddAgentRuntime(this IServiceCollection services)
    {
        services.AddSingleton<IAgentDefinitionLoader, AgentDefinitionLoader>();
        services.AddSingleton<IPromptLoader, PromptLoader>();
        services.AddSingleton<SchemaLoader>();
        services.AddSingleton<ILlmProvider, MockLlmProvider>();
        services.AddScoped<IAgentExecutor, AgentExecutor>();
        return services;
    }
}
