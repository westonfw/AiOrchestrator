using System.Text.Json;
using System.Text.Json.Serialization;
using AiOrchestrator.Application;
using AiOrchestrator.Agents;
using AiOrchestrator.Infrastructure;
using AiOrchestrator.Skills;
using AiOrchestrator.Workflow;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

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

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        var json = JsonSerializer.Serialize(
            ApiResponse<object>.Fail("internal_error", "An unexpected error occurred."),
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await context.Response.WriteAsync(json);
    });
});

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
    else
    {
        await db.Database.MigrateAsync();
    }
}

app.MapControllers();

app.Run();

public partial class Program;
