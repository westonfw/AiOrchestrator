using AiOrchestrator.Agents;
using AiOrchestrator.Infrastructure;
using AiOrchestrator.Skills;
using AiOrchestrator.Workflow;
using AiOrchestrator.Worker;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSkillRuntime();
builder.Services.AddAgentRuntime(builder.Configuration);
builder.Services.AddWorkflowRuntime();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

// Apply pending EF migrations before starting to consume messages.
// MigrateAsync is idempotent and uses an advisory lock — safe to call from multiple instances.
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AiOrchestratorDbContext>();
    if (!db.Database.IsInMemory())
    {
        await db.Database.MigrateAsync();
    }
}

await host.RunAsync();
