using System.Text.Json.Nodes;
using AiOrchestrator.Application;
using AiOrchestrator.Domain;
using TaskStatus = AiOrchestrator.Domain.TaskStatus;

namespace AiOrchestrator.Workflow;

public sealed class WorkflowExecutor(
    IOrchestrationStore store,
    IWorkflowDefinitionLoader definitionLoader,
    ISkillExecutor skillExecutor,
    IAgentExecutor agentExecutor) : IWorkflowExecutor
{
    public async Task<Guid> StartAsync(Guid taskId, CancellationToken ct = default)
    {
        var task = await store.FindTaskAsync(taskId, ct) ?? throw new InvalidOperationException($"Task '{taskId}' not found.");
        var definition = await definitionLoader.LoadAsync(task.ScenarioCode, ct);

        var context = new JsonObject
        {
            ["input"] = JsonSupport.ParseObject(task.InputJson),
            ["steps"] = new JsonObject()
        };

        var workflowRun = new WorkflowRun
        {
            TaskId = task.Id,
            WorkflowCode = definition.Code,
            WorkflowVersion = definition.Version,
            Status = WorkflowRunStatus.Running,
            ContextJson = JsonSupport.ToJson(context),
            StartedAt = DateTimeOffset.UtcNow
        };

        task.Status = TaskStatus.Running;
        task.CurrentStep = null;
        task.UpdatedAt = DateTimeOffset.UtcNow;

        await store.AddWorkflowRunAsync(workflowRun, ct);
        await store.AddTraceAsync(new TraceEvent
        {
            TaskId = task.Id,
            WorkflowRunId = workflowRun.Id,
            EventType = "workflow_started",
            Message = $"Workflow {definition.Code} started."
        }, ct);
        await store.SaveChangesAsync(ct);

        await ContinueAsync(workflowRun.Id, ct);
        return workflowRun.Id;
    }

    public async Task ContinueAsync(Guid workflowRunId, CancellationToken ct = default)
    {
        var workflowRun = await store.FindWorkflowRunAsync(workflowRunId, ct)
            ?? throw new InvalidOperationException($"Workflow run '{workflowRunId}' not found.");
        var task = await store.FindTaskAsync(workflowRun.TaskId, ct)
            ?? throw new InvalidOperationException($"Task '{workflowRun.TaskId}' not found.");
        var definition = await definitionLoader.LoadAsync(task.ScenarioCode, ct);

        workflowRun.Status = WorkflowRunStatus.Running;
        task.Status = TaskStatus.Running;
        task.UpdatedAt = DateTimeOffset.UtcNow;
        await store.SaveChangesAsync(ct);

        while (true)
        {
            var stepRuns = await store.ListStepRunsAsync(workflowRun.Id, ct);
            var nextStep = definition.Steps.FirstOrDefault(step =>
                !stepRuns.Any(run => run.StepId == step.Id && run.Status is StepRunStatus.Succeeded or StepRunStatus.WaitingReview)
                && step.DependsOn.All(dep => stepRuns.Any(run => run.StepId == dep && run.Status == StepRunStatus.Succeeded)));

            if (nextStep is null)
            {
                if (definition.Steps.All(step => stepRuns.Any(run => run.StepId == step.Id && run.Status == StepRunStatus.Succeeded)))
                {
                    workflowRun.Status = WorkflowRunStatus.Succeeded;
                    workflowRun.FinishedAt = DateTimeOffset.UtcNow;
                    task.Status = TaskStatus.Succeeded;
                    task.CurrentStep = null;
                    task.UpdatedAt = DateTimeOffset.UtcNow;
                    await store.AddTraceAsync(new TraceEvent
                    {
                        TaskId = task.Id,
                        WorkflowRunId = workflowRun.Id,
                        EventType = "workflow_finished",
                        Message = "Workflow succeeded."
                    }, ct);
                    await store.SaveChangesAsync(ct);
                }

                return;
            }

            var executionContext = BuildContext(task, workflowRun, stepRuns);
            var stepRun = new WorkflowStepRun
            {
                TaskId = task.Id,
                WorkflowRunId = workflowRun.Id,
                StepId = nextStep.Id,
                StepName = nextStep.Name,
                StepType = nextStep.Type,
                Status = StepRunStatus.Running,
                StartedAt = DateTimeOffset.UtcNow,
                InputJson = JsonSupport.ToJson(executionContext.Context)
            };

            task.CurrentStep = nextStep.Id;
            task.UpdatedAt = DateTimeOffset.UtcNow;
            await store.AddStepRunAsync(stepRun, ct);
            await store.AddTraceAsync(new TraceEvent
            {
                TaskId = task.Id,
                WorkflowRunId = workflowRun.Id,
                StepRunId = stepRun.Id,
                EventType = "step_started",
                Message = $"Step {nextStep.Id} started."
            }, ct);
            await store.SaveChangesAsync(ct);

            var result = await ExecuteStepAsync(executionContext, nextStep, stepRun, ct);

            stepRun.OutputJson = JsonSupport.ToJson(result.Output);
            stepRun.ErrorMessage = result.ErrorMessage;
            stepRun.FinishedAt = DateTimeOffset.UtcNow;

            if (result.WaitingReview)
            {
                stepRun.Status = StepRunStatus.WaitingReview;
                workflowRun.Status = WorkflowRunStatus.WaitingReview;
                task.Status = TaskStatus.WaitingReview;
                task.CurrentStep = nextStep.Id;
                task.UpdatedAt = DateTimeOffset.UtcNow;
                MergeStepOutput(workflowRun, nextStep.Id, result.Output);
                await store.AddTraceAsync(new TraceEvent
                {
                    TaskId = task.Id,
                    WorkflowRunId = workflowRun.Id,
                    StepRunId = stepRun.Id,
                    EventType = "review_required",
                    Message = $"Step {nextStep.Id} is waiting for human review."
                }, ct);
                await store.SaveChangesAsync(ct);
                return;
            }

            if (!result.Success)
            {
                stepRun.Status = StepRunStatus.Failed;
                workflowRun.Status = WorkflowRunStatus.Failed;
                workflowRun.FinishedAt = DateTimeOffset.UtcNow;
                task.Status = TaskStatus.Failed;
                task.CurrentStep = nextStep.Id;
                task.UpdatedAt = DateTimeOffset.UtcNow;
                await store.AddTraceAsync(new TraceEvent
                {
                    TaskId = task.Id,
                    WorkflowRunId = workflowRun.Id,
                    StepRunId = stepRun.Id,
                    EventType = "error",
                    Message = result.ErrorMessage
                }, ct);
                await store.SaveChangesAsync(ct);
                return;
            }

            stepRun.Status = StepRunStatus.Succeeded;
            MergeStepOutput(workflowRun, nextStep.Id, result.Output);
            await store.AddTraceAsync(new TraceEvent
            {
                TaskId = task.Id,
                WorkflowRunId = workflowRun.Id,
                StepRunId = stepRun.Id,
                EventType = "step_finished",
                Message = $"Step {nextStep.Id} succeeded."
            }, ct);
            await store.SaveChangesAsync(ct);
        }
    }

    private async Task<StepExecutionResult> ExecuteStepAsync(
        WorkflowExecutionContext context,
        WorkflowStepDefinition step,
        WorkflowStepRun stepRun,
        CancellationToken ct)
    {
        return step.Type switch
        {
            "skill" => await skillExecutor.ExecuteAsync(context, step, stepRun, ct),
            "agent" => await agentExecutor.ExecuteAsync(context, step, stepRun, ct),
            "review" => await CreateReviewAsync(context, step, stepRun, ct),
            _ => StepExecutionResult.Failed($"Unsupported workflow step type '{step.Type}'.")
        };
    }

    private async Task<StepExecutionResult> CreateReviewAsync(
        WorkflowExecutionContext context,
        WorkflowStepDefinition step,
        WorkflowStepRun stepRun,
        CancellationToken ct)
    {
        var reviewContent = new JsonObject
        {
            ["step_id"] = step.Id,
            ["workflow_context"] = JsonSupport.CloneObject(context.Context),
            ["rating_committee"] = JsonSupport.CloneNode(context.StepOutputs.GetValueOrDefault("rating_committee")),
            ["devil_review"] = JsonSupport.CloneNode(context.StepOutputs.GetValueOrDefault("devil_review"))
        };

        var review = new ReviewItem
        {
            TaskId = context.TaskId,
            StepRunId = stepRun.Id,
            Title = step.Name,
            ContentJson = JsonSupport.ToJson(reviewContent),
            Status = ReviewStatus.Pending
        };

        await store.AddReviewAsync(review, ct);
        return StepExecutionResult.Waiting(new JsonObject
        {
            ["review_id"] = review.Id.ToString(),
            ["status"] = ReviewStatus.Pending.ToString()
        });
    }

    private static WorkflowExecutionContext BuildContext(AiTask task, WorkflowRun workflowRun, IEnumerable<WorkflowStepRun> stepRuns)
    {
        var context = JsonSupport.ParseObject(workflowRun.ContextJson);
        if (!context.ContainsKey("input"))
        {
            context["input"] = JsonSupport.ParseObject(task.InputJson);
        }

        if (!context.ContainsKey("steps"))
        {
            context["steps"] = new JsonObject();
        }

        var outputs = stepRuns
            .Where(x => x.Status == StepRunStatus.Succeeded && !string.IsNullOrWhiteSpace(x.OutputJson))
            .ToDictionary(x => x.StepId, x => JsonSupport.ParseNode(x.OutputJson));

        return new WorkflowExecutionContext
        {
            TaskId = task.Id,
            WorkflowRunId = workflowRun.Id,
            ScenarioCode = task.ScenarioCode,
            Input = JsonSupport.ParseObject(task.InputJson),
            Context = context,
            StepOutputs = outputs
        };
    }

    private static void MergeStepOutput(WorkflowRun workflowRun, string stepId, JsonNode? output)
    {
        var context = JsonSupport.ParseObject(workflowRun.ContextJson);
        if (context["steps"] is not JsonObject steps)
        {
            steps = new JsonObject();
            context["steps"] = steps;
        }

        steps[stepId] = JsonSupport.CloneNode(output) ?? new JsonObject();
        workflowRun.ContextJson = JsonSupport.ToJson(context);
    }
}
