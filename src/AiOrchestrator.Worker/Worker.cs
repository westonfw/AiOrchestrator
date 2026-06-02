using System.Text;
using System.Text.Json;
using AiOrchestrator.Application;
using AiOrchestrator.Infrastructure;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace AiOrchestrator.Worker;

public sealed class Worker(
    IServiceScopeFactory scopeFactory,
    IOptions<RabbitMqOptions> rabbitOptions,
    ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opts = rabbitOptions.Value;
        var factory = new ConnectionFactory
        {
            HostName = opts.Host,
            Port = opts.Port,
            UserName = opts.Username,
            Password = opts.Password
        };

        IConnection? connection = null;
        while (connection is null && !stoppingToken.IsCancellationRequested)
        {
            try
            {
                connection = await factory.CreateConnectionAsync(stoppingToken);
                logger.LogInformation("Connected to RabbitMQ at {Host}:{Port}.", opts.Host, opts.Port);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "RabbitMQ not ready, retrying in 3 s...");
                try { await Task.Delay(3_000, stoppingToken); } catch (OperationCanceledException) { }
            }
        }

        if (connection is null) return;

        await using (connection)
        {
            var startChannel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
            await startChannel.QueueDeclareAsync(opts.StartWorkflowQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
            await startChannel.BasicQosAsync(0, 1, false, stoppingToken);

            var continueChannel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
            await continueChannel.QueueDeclareAsync(opts.ContinueWorkflowQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
            await continueChannel.BasicQosAsync(0, 1, false, stoppingToken);

            var startConsumer = new AsyncEventingBasicConsumer(startChannel);
            startConsumer.ReceivedAsync += (_, ea) => HandleStartAsync(ea, startChannel, stoppingToken);
            await startChannel.BasicConsumeAsync(opts.StartWorkflowQueue, autoAck: false, consumer: startConsumer, cancellationToken: stoppingToken);

            var continueConsumer = new AsyncEventingBasicConsumer(continueChannel);
            continueConsumer.ReceivedAsync += (_, ea) => HandleContinueAsync(ea, continueChannel, stoppingToken);
            await continueChannel.BasicConsumeAsync(opts.ContinueWorkflowQueue, autoAck: false, consumer: continueConsumer, cancellationToken: stoppingToken);

            logger.LogInformation("Worker listening on '{Start}' and '{Continue}'.", opts.StartWorkflowQueue, opts.ContinueWorkflowQueue);

            try { await Task.Delay(Timeout.Infinite, stoppingToken); } catch (OperationCanceledException) { }
        }
    }

    private async Task HandleStartAsync(BasicDeliverEventArgs ea, IChannel channel, CancellationToken ct)
    {
        var body = Encoding.UTF8.GetString(ea.Body.Span);
        try
        {
            var msg = JsonSerializer.Deserialize<StartWorkflowMessage>(body)
                ?? throw new InvalidOperationException("Null message body.");

            logger.LogInformation("Starting workflow for task {TaskId}.", msg.TaskId);

            await using var scope = scopeFactory.CreateAsyncScope();
            var executor = scope.ServiceProvider.GetRequiredService<IWorkflowExecutor>();
            await executor.StartAsync(msg.TaskId, ct);

            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process start message: {Body}", body);
            await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: ct);
        }
    }

    private async Task HandleContinueAsync(BasicDeliverEventArgs ea, IChannel channel, CancellationToken ct)
    {
        var body = Encoding.UTF8.GetString(ea.Body.Span);
        try
        {
            var msg = JsonSerializer.Deserialize<ContinueWorkflowMessage>(body)
                ?? throw new InvalidOperationException("Null message body.");

            logger.LogInformation("Continuing workflow run {WorkflowRunId}.", msg.WorkflowRunId);

            await using var scope = scopeFactory.CreateAsyncScope();
            var executor = scope.ServiceProvider.GetRequiredService<IWorkflowExecutor>();
            await executor.ContinueAsync(msg.WorkflowRunId, ct);

            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process continue message: {Body}", body);
            await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: ct);
        }
    }
}
