using AiOrchestrator.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AiOrchestrator.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AiOrchestratorDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("Postgres");
            var configuredUseInMemory = configuration["Database:UseInMemory"];
            var useInMemory = string.IsNullOrWhiteSpace(configuredUseInMemory)
                ? string.IsNullOrWhiteSpace(connectionString)
                : bool.TryParse(configuredUseInMemory, out var parsed) && parsed;

            if (useInMemory)
            {
                options.UseInMemoryDatabase(configuration["Database:InMemoryName"] ?? "ai-orchestrator");
            }
            else
            {
                options.UseNpgsql(connectionString);
            }
        });

        services.AddScoped<IOrchestrationStore, EfOrchestrationStore>();
        services.AddSingleton<IJsonSchemaValidator, BasicJsonSchemaValidator>();
        return services;
    }
}
