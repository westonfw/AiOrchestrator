using AiOrchestrator.Application;
using Microsoft.Extensions.DependencyInjection;

namespace AiOrchestrator.Workflow;

public static class DependencyInjection
{
    public static IServiceCollection AddWorkflowRuntime(this IServiceCollection services)
    {
        services.AddSingleton<IWorkflowDefinitionLoader, WorkflowDefinitionLoader>();
        services.AddScoped<IWorkflowExecutor, WorkflowExecutor>();
        return services;
    }
}
