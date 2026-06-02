using AiOrchestrator.Application;
using AiOrchestrator.Domain;
using Microsoft.AspNetCore.Mvc;

namespace AiOrchestrator.Api.Controllers;

[ApiController]
[Route("api/tasks")]
public sealed class TasksController : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ApiResponse<object>>> CreateAsync(
        CreateTaskRequest request,
        IOrchestrationStore store,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.ScenarioCode) || string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(ApiResponse<object>.Fail("invalid_request", "scenarioCode and title are required."));
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

        return Ok(ApiResponse<object>.Ok(new { task.Id, Status = task.Status.ToString() }));
    }

    [HttpPost("{taskId:guid}/start")]
    public async Task<ActionResult<ApiResponse<object>>> StartAsync(
        Guid taskId,
        IWorkflowExecutor executor,
        CancellationToken ct)
    {
        var workflowRunId = await executor.StartAsync(taskId, ct);
        return Ok(ApiResponse<object>.Ok(new { WorkflowRunId = workflowRunId }));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> ListAsync(
        [FromQuery] string? scenarioCode,
        [FromQuery] string? status,
        [FromQuery] int? pageIndex,
        [FromQuery] int? pageSize,
        IOrchestrationStore store,
        CancellationToken ct)
    {
        var tasks = await store.ListTasksAsync(scenarioCode, status, pageIndex ?? 1, pageSize ?? 20, ct);
        return Ok(ApiResponse<object>.Ok(new { Items = tasks }));
    }

    [HttpGet("{taskId:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> GetAsync(
        Guid taskId,
        IOrchestrationStore store,
        CancellationToken ct)
    {
        var task = await store.FindTaskAsync(taskId, ct);
        if (task is null)
        {
            return NotFound(ApiResponse<object>.Fail("not_found", "Task not found."));
        }

        var workflowRun = await store.FindLatestWorkflowRunAsync(taskId, ct);
        var steps = workflowRun is null ? [] : await store.ListStepRunsAsync(workflowRun.Id, ct);
        var reviews = await store.ListReviewsAsync(taskId, null, ct);
        var artifacts = await store.ListArtifactsAsync(taskId, ct);
        var evidence = await store.ListEvidenceAsync(taskId, ct);

        return Ok(ApiResponse<object>.Ok(new
        {
            Task = task,
            WorkflowRun = workflowRun,
            Steps = steps,
            Reviews = reviews,
            Artifacts = artifacts,
            Evidence = evidence
        }));
    }
}
