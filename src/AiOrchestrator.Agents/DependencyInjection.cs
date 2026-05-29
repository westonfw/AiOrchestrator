using AiOrchestrator.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AiOrchestrator.Agents;

public static class DependencyInjection
{
    public static IServiceCollection AddAgentRuntime(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IAgentDefinitionLoader, AgentDefinitionLoader>();
        services.AddSingleton<IPromptLoader, PromptLoader>();
        services.AddSingleton<SchemaLoader>();
        services.AddSingleton<ILlmProvider>(serviceProvider =>
        {
            var provider = configuration["Llm:Provider"] ?? "Mock";
            return string.Equals(provider, "OpenAICompatible", StringComparison.OrdinalIgnoreCase)
                ? new OpenAICompatibleLlmProvider(serviceProvider.GetRequiredService<IConfiguration>())
                : new MockLlmProvider();
        });
        services.AddScoped<IAgentExecutor, AgentExecutor>();
        return services;
    }
}
