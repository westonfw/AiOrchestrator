namespace AiOrchestrator.Infrastructure;

public sealed class RabbitMqOptions
{
    public bool Enabled { get; set; }
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string StartWorkflowQueue { get; set; } = "workflow.start";
    public string ContinueWorkflowQueue { get; set; } = "workflow.continue";
}
