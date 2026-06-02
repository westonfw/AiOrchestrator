using System.Text.Json.Nodes;
using AiOrchestrator.Application;
using AiOrchestrator.Domain;
using Microsoft.AspNetCore.Mvc;
using TaskStatus = AiOrchestrator.Domain.TaskStatus;

namespace AiOrchestrator.Api.Controllers;

[ApiController]
[Route("api/reviews")]
public sealed class ReviewsController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> ListAsync(
        [FromQuery] Guid? taskId,
        [FromQuery] string? status,
        IOrchestrationStore store,
        CancellationToken ct)
    {
        ReviewStatus? parsedStatus = Enum.TryParse<ReviewStatus>(status, true, out var value) ? value : null;
        var reviews = await store.ListReviewsAsync(taskId, parsedStatus, ct);
        return Ok(ApiResponse<object>.Ok(new { Items = reviews }));
    }

    [HttpPost("{reviewId:guid}/approve")]
    public Task<ActionResult<ApiResponse<object>>> ApproveAsync(
        Guid reviewId,
        ReviewActionRequest? request,
        IOrchestrationStore store,
        IWorkflowExecutor executor,
        CancellationToken ct) =>
        CompleteReviewAsync(reviewId, ReviewStatus.Approved, request, store, executor, ct);

    [HttpPost("{reviewId:guid}/reject")]
    public async Task<ActionResult<ApiResponse<object>>> RejectAsync(
        Guid reviewId,
        ReviewActionRequest? request,
        IOrchestrationStore store,
        CancellationToken ct)
    {
        var review = await store.FindReviewAsync(reviewId, ct);
        if (review is null)
        {
            return NotFound(ApiResponse<object>.Fail("not_found", "Review not found."));
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
        return Ok(ApiResponse<object>.Ok(new { review.Id, Status = review.Status.ToString() }));
    }

    [HttpPost("{reviewId:guid}/modify")]
    public Task<ActionResult<ApiResponse<object>>> ModifyAsync(
        Guid reviewId,
        ReviewActionRequest? request,
        IOrchestrationStore store,
        IWorkflowExecutor executor,
        CancellationToken ct) =>
        CompleteReviewAsync(reviewId, ReviewStatus.Modified, request, store, executor, ct);

    private async Task<ActionResult<ApiResponse<object>>> CompleteReviewAsync(
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
            return NotFound(ApiResponse<object>.Fail("not_found", "Review not found."));
        }

        var workflowRun = await store.FindLatestWorkflowRunAsync(review.TaskId, ct);
        var task = await store.FindTaskAsync(review.TaskId, ct);
        var stepRun = await store.FindStepRunAsync(review.StepRunId, ct);
        if (workflowRun is null || task is null || stepRun is null)
        {
            return BadRequest(ApiResponse<object>.Fail("invalid_state", "Review is missing workflow/task/step linkage."));
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
        return Ok(ApiResponse<object>.Ok(new { review.Id, Status = review.Status.ToString(), WorkflowRunId = workflowRun.Id }));
    }
}
