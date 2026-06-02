using AiOrchestrator.Application;

namespace AiOrchestrator.Infrastructure;

// Used when RabbitMq:Enabled is false (dev / integration tests).
// Executes the workflow synchronously in the calling request scope.
public sealed class InProcessTaskQueue(IWorkflowExecutor executor) : ITaskQueue
{
    public Task EnqueueStartWorkflowAsync(Guid taskId, CancellationToken ct = default)
        => executor.StartAsync(taskId, ct);

    public Task EnqueueContinueWorkflowAsync(Guid workflowRunId, CancellationToken ct = default)
        => executor.ContinueAsync(workflowRunId, ct);
}
