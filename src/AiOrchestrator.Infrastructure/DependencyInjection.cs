using AiOrchestrator.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

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
        services.AddHttpClient<IPublicMarketDataProvider, PublicMarketDataProvider>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(20);
        });

        services.Configure<RabbitMqOptions>(configuration.GetSection("RabbitMq"));

        if (configuration.GetValue<bool>("RabbitMq:Enabled"))
        {
            // Singleton RabbitMQ connection; blocks at startup (with retry) until RabbitMQ is available.
            services.AddSingleton<IConnection>(sp =>
            {
                var opts = sp.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
                var factory = new ConnectionFactory
                {
                    HostName = opts.Host,
                    Port = opts.Port,
                    UserName = opts.Username,
                    Password = opts.Password
                };
                Exception? last = null;
                for (int attempt = 0; attempt < 15; attempt++)
                {
                    try
                    {
                        return factory.CreateConnectionAsync().GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        last = ex;
                        Thread.Sleep(3_000);
                    }
                }
                throw new InvalidOperationException($"Cannot connect to RabbitMQ at {opts.Host}:{opts.Port}.", last);
            });
            services.AddScoped<ITaskQueue, RabbitMqTaskQueue>();
        }
        else
        {
            services.AddScoped<ITaskQueue, InProcessTaskQueue>();
        }

        return services;
    }
}
