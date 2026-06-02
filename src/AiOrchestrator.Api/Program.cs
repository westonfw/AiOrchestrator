using System.Text.Json.Serialization;
using AiOrchestrator.Agents;
using AiOrchestrator.Infrastructure;
using AiOrchestrator.Skills;
using AiOrchestrator.Workflow;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSkillRuntime();
builder.Services.AddAgentRuntime(builder.Configuration);
builder.Services.AddWorkflowRuntime();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AiOrchestratorDbContext>();
    if (db.Database.IsInMemory())
    {
        db.Database.EnsureCreated();
    }
}

app.MapControllers();

app.Run();

public partial class Program;
