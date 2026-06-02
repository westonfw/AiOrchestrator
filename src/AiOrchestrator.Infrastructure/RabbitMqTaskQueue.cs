using System.Text;
using System.Text.Json;
using AiOrchestrator.Application;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace AiOrchestrator.Infrastructure;

public sealed record StartWorkflowMessage(Guid TaskId);
public sealed record ContinueWorkflowMessage(Guid WorkflowRunId);

// Used when RabbitMq:Enabled is true. Publishes task IDs to RabbitMQ;
// the Worker process picks them up and calls WorkflowExecutor.
public sealed class RabbitMqTaskQueue(IConnection connection, IOptions<RabbitMqOptions> options) : ITaskQueue
{
    private readonly RabbitMqOptions _opts = options.Value;

    public async Task EnqueueStartWorkflowAsync(Guid taskId, CancellationToken ct = default)
    {
        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
        await channel.QueueDeclareAsync(_opts.StartWorkflowQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        var body = JsonSerializer.SerializeToUtf8Bytes(new StartWorkflowMessage(taskId));
        await channel.BasicPublishAsync(string.Empty, _opts.StartWorkflowQueue, body, ct);
    }

    public async Task EnqueueContinueWorkflowAsync(Guid workflowRunId, CancellationToken ct = default)
    {
        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
        await channel.QueueDeclareAsync(_opts.ContinueWorkflowQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        var body = JsonSerializer.SerializeToUtf8Bytes(new ContinueWorkflowMessage(workflowRunId));
        await channel.BasicPublishAsync(string.Empty, _opts.ContinueWorkflowQueue, body, ct);
    }
}
