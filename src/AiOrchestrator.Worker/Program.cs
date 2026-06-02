using AiOrchestrator.Agents;
using AiOrchestrator.Infrastructure;
using AiOrchestrator.Skills;
using AiOrchestrator.Workflow;
using AiOrchestrator.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSkillRuntime();
builder.Services.AddAgentRuntime(builder.Configuration);
builder.Services.AddWorkflowRuntime();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

// Migrations are applied by the API on startup.
// Worker only waits for the database to be reachable before processing messages.
await host.RunAsync();
