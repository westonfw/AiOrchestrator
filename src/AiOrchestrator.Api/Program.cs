using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using AiOrchestrator.Agents;
using AiOrchestrator.Application;
using AiOrchestrator.Domain;
using AiOrchestrator.Infrastructure;
using AiOrchestrator.Skills;
using AiOrchestrator.Workflow;
using Microsoft.EntityFrameworkCore;
using TaskStatus = AiOrchestrator.Domain.TaskStatus;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
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

app.MapPost("/api/tasks", async (CreateTaskRequest request, IOrchestrationStore store, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.ScenarioCode) || string.IsNullOrWhiteSpace(request.Title))
    {
        return Results.BadRequest(ApiResponse<object>.Fail("invalid_request", "scenarioCode and title are required."));
    }

    var task = new AiTask
    {
        ScenarioCode = request.ScenarioCode,
        Title = request.Title,
        InputJson = JsonSupport.ToJson(request.Input),
        CreatedBy = request.CreatedBy
    };
    await store.AddTaskAsync(task, ct);
    await store.AddTraceAsync(new TraceEvent
    {
        TaskId = task.Id,
        EventType = "task_created",
        Message = "Task created."
    }, ct);
    await store.SaveChangesAsync(ct);

    return Results.Ok(ApiResponse<object>.Ok(new { task.Id, Status = task.Status.ToString() }));
});

app.MapPost("/api/tasks/{taskId:guid}/start", async (Guid taskId, IWorkflowExecutor executor, CancellationToken ct) =>
{
    var workflowRunId = await executor.StartAsync(taskId, ct);
    return Results.Ok(ApiResponse<object>.Ok(new { WorkflowRunId = workflowRunId }));
});

app.MapGet("/api/tasks", async (
    string? scenarioCode,
    string? status,
    int? pageIndex,
    int? pageSize,
    IOrchestrationStore store,
    CancellationToken ct) =>
{
    var tasks = await store.ListTasksAsync(scenarioCode, status, pageIndex ?? 1, pageSize ?? 20, ct);
    return Results.Ok(ApiResponse<object>.Ok(new { Items = tasks }));
});

app.MapGet("/api/tasks/{taskId:guid}", async (Guid taskId, IOrchestrationStore store, CancellationToken ct) =>
{
    var task = await store.FindTaskAsync(taskId, ct);
    if (task is null)
    {
        return Results.NotFound(ApiResponse<object>.Fail("not_found", "Task not found."));
    }

    var workflowRun = await store.FindLatestWorkflowRunAsync(taskId, ct);
    var steps = workflowRun is null ? [] : await store.ListStepRunsAsync(workflowRun.Id, ct);
    var reviews = await store.ListReviewsAsync(taskId, null, ct);
    var artifacts = await store.ListArtifactsAsync(taskId, ct);
    var evidence = await store.ListEvidenceAsync(taskId, ct);

    return Results.Ok(ApiResponse<object>.Ok(new
    {
        Task = task,
        WorkflowRun = workflowRun,
        Steps = steps,
        Reviews = reviews,
        Artifacts = artifacts,
        Evidence = evidence
    }));
});

app.MapGet("/api/reviews", async (Guid? taskId, string? status, IOrchestrationStore store, CancellationToken ct) =>
{
    ReviewStatus? parsedStatus = Enum.TryParse<ReviewStatus>(status, true, out var value) ? value : null;
    var reviews = await store.ListReviewsAsync(taskId, parsedStatus, ct);
    return Results.Ok(ApiResponse<object>.Ok(new { Items = reviews }));
});

app.MapPost("/api/reviews/{reviewId:guid}/approve", async (
    Guid reviewId,
    ReviewActionRequest? request,
    IOrchestrationStore store,
    IWorkflowExecutor executor,
    CancellationToken ct) =>
    await CompleteReviewAsync(reviewId, ReviewStatus.Approved, request, store, executor, ct));

app.MapPost("/api/reviews/{reviewId:guid}/reject", async (
    Guid reviewId,
    ReviewActionRequest? request,
    IOrchestrationStore store,
    CancellationToken ct) =>
{
    var review = await store.FindReviewAsync(reviewId, ct);
    if (review is null)
    {
        return Results.NotFound(ApiResponse<object>.Fail("not_found", "Review not found."));
    }

    var task = await store.FindTaskAsync(review.TaskId, ct);
    var workflowRun = await store.FindLatestWorkflowRunAsync(review.TaskId, ct);
    var stepRun = await store.FindStepRunAsync(review.StepRunId, ct);
    review.Status = ReviewStatus.Rejected;
    review.ReviewComment = request?.Comment;
    review.ReviewedAt = DateTimeOffset.UtcNow;

    if (stepRun is not null)
    {
        stepRun.Status = StepRunStatus.Failed;
        stepRun.ErrorMessage = request?.Comment ?? "Human review rejected.";
        stepRun.FinishedAt = DateTimeOffset.UtcNow;
    }

    if (workflowRun is not null)
    {
        workflowRun.Status = WorkflowRunStatus.Failed;
        workflowRun.FinishedAt = DateTimeOffset.UtcNow;
    }

    if (task is not null)
    {
        task.Status = TaskStatus.Failed;
        task.UpdatedAt = DateTimeOffset.UtcNow;
    }

    await store.SaveChangesAsync(ct);
    return Results.Ok(ApiResponse<object>.Ok(new { review.Id, Status = review.Status.ToString() }));
});

app.MapPost("/api/reviews/{reviewId:guid}/modify", async (
    Guid reviewId,
    ReviewActionRequest? request,
    IOrchestrationStore store,
    IWorkflowExecutor executor,
    CancellationToken ct) =>
    await CompleteReviewAsync(reviewId, ReviewStatus.Modified, request, store, executor, ct));

app.MapGet("/api/tasks/{taskId:guid}/artifacts", async (Guid taskId, IOrchestrationStore store, CancellationToken ct) =>
{
    var artifacts = await store.ListArtifactsAsync(taskId, ct);
    return Results.Ok(ApiResponse<object>.Ok(new { Items = artifacts }));
});

app.MapGet("/api/artifacts/{artifactId:guid}", async (Guid artifactId, IOrchestrationStore store, CancellationToken ct) =>
{
    var artifact = await store.FindArtifactAsync(artifactId, ct);
    return artifact is null
        ? Results.NotFound(ApiResponse<object>.Fail("not_found", "Artifact not found."))
        : Results.Ok(ApiResponse<object>.Ok(artifact));
});

app.MapGet("/api/tasks/{taskId:guid}/evidence", async (Guid taskId, IOrchestrationStore store, CancellationToken ct) =>
{
    var evidence = await store.ListEvidenceAsync(taskId, ct);
    return Results.Ok(ApiResponse<object>.Ok(new { Items = evidence }));
});

app.MapPost("/api/evidence/{evidenceId:guid}/verify", async (Guid evidenceId, IOrchestrationStore store, CancellationToken ct) =>
{
    var evidence = await store.FindEvidenceAsync(evidenceId, ct);
    if (evidence is null)
    {
        return Results.NotFound(ApiResponse<object>.Fail("not_found", "Evidence not found."));
    }

    evidence.Verified = true;
    await store.SaveChangesAsync(ct);
    return Results.Ok(ApiResponse<object>.Ok(new { evidence.Id, evidence.Verified }));
});

app.MapGet("/api/scenarios/{scenarioCode}/workflow", async (string scenarioCode, IWorkflowDefinitionLoader loader, CancellationToken ct) =>
{
    var workflow = await loader.LoadAsync(scenarioCode, ct);
    return Results.Ok(ApiResponse<object>.Ok(workflow));
});

app.MapGet("/api/tasks/{taskId:guid}/trace", async (Guid taskId, IOrchestrationStore store, CancellationToken ct) =>
{
    var trace = await store.ListTraceAsync(taskId, ct);
    return Results.Ok(ApiResponse<object>.Ok(new { Items = trace }));
});

app.MapGet("/api/public-data/companies/search", async (
    string keyword,
    string? market,
    IPublicMarketDataProvider publicDataProvider,
    CancellationToken ct) =>
{
    var results = await publicDataProvider.SearchCompaniesAsync(keyword, market, ct);
    return Results.Ok(ApiResponse<object>.Ok(new { Items = results }));
});

app.MapGet("/api/public-data/companies/{symbol}", async (
    string symbol,
    string? market,
    IPublicMarketDataProvider publicDataProvider,
    CancellationToken ct) =>
{
    var profile = await publicDataProvider.GetCompanyProfileAsync(symbol, market, ct);
    return profile is null
        ? Results.NotFound(ApiResponse<object>.Fail("not_found", "Company profile not found."))
        : Results.Ok(ApiResponse<object>.Ok(profile));
});

app.MapGet("/api/public-data/quotes/{symbol}", async (
    string symbol,
    string? market,
    IPublicMarketDataProvider publicDataProvider,
    CancellationToken ct) =>
{
    var quote = await publicDataProvider.GetLatestQuoteAsync(symbol, market, ct);
    return quote is null
        ? Results.NotFound(ApiResponse<object>.Fail("not_found", "Quote not found from configured public data source."))
        : Results.Ok(ApiResponse<object>.Ok(quote));
});

app.Run();

static async Task<IResult> CompleteReviewAsync(
    Guid reviewId,
    ReviewStatus reviewStatus,
    ReviewActionRequest? request,
    IOrchestrationStore store,
    IWorkflowExecutor executor,
    CancellationToken ct)
{
    var review = await store.FindReviewAsync(reviewId, ct);
    if (review is null)
    {
        return Results.NotFound(ApiResponse<object>.Fail("not_found", "Review not found."));
    }

    var workflowRun = await store.FindLatestWorkflowRunAsync(review.TaskId, ct);
    var task = await store.FindTaskAsync(review.TaskId, ct);
    var stepRun = await store.FindStepRunAsync(review.StepRunId, ct);
    if (workflowRun is null || task is null || stepRun is null)
    {
        return Results.BadRequest(ApiResponse<object>.Fail("invalid_state", "Review is missing workflow/task/step linkage."));
    }

    var output = new JsonObject
    {
        ["review_id"] = review.Id.ToString(),
        ["status"] = reviewStatus.ToString(),
        ["comment"] = request?.Comment
    };
    if (request?.ModifiedContent is not null)
    {
        output["modified_content"] = JsonSupport.CloneObject(request.ModifiedContent);
        review.ContentJson = JsonSupport.ToJson(request.ModifiedContent);
    }

    review.Status = reviewStatus;
    review.ReviewComment = request?.Comment;
    review.ReviewedAt = DateTimeOffset.UtcNow;

    stepRun.Status = StepRunStatus.Succeeded;
    stepRun.OutputJson = JsonSupport.ToJson(output);
    stepRun.ErrorMessage = null;
    stepRun.FinishedAt = DateTimeOffset.UtcNow;

    var context = JsonSupport.ParseObject(workflowRun.ContextJson);
    if (context["steps"] is not JsonObject steps)
    {
        steps = new JsonObject();
        context["steps"] = steps;
    }

    steps[stepRun.StepId] = JsonSupport.CloneNode(output);
    workflowRun.ContextJson = JsonSupport.ToJson(context);
    workflowRun.Status = WorkflowRunStatus.Running;
    task.Status = TaskStatus.Running;
    task.UpdatedAt = DateTimeOffset.UtcNow;

    await store.AddTraceAsync(new TraceEvent
    {
        TaskId = task.Id,
        WorkflowRunId = workflowRun.Id,
        StepRunId = stepRun.Id,
        EventType = "review_approved",
        Message = $"Review {reviewStatus}."
    }, ct);
    await store.SaveChangesAsync(ct);

    await executor.ContinueAsync(workflowRun.Id, ct);
    return Results.Ok(ApiResponse<object>.Ok(new { review.Id, Status = review.Status.ToString(), WorkflowRunId = workflowRun.Id }));
}

public partial class Program;
