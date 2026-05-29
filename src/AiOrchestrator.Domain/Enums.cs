namespace AiOrchestrator.Domain;

public enum TaskStatus
{
    Created,
    Running,
    WaitingReview,
    Succeeded,
    Failed,
    Cancelled
}

public enum WorkflowRunStatus
{
    Created,
    Running,
    WaitingReview,
    Succeeded,
    Failed,
    Cancelled
}

public enum StepRunStatus
{
    Pending,
    Running,
    Succeeded,
    Failed,
    WaitingReview,
    Skipped
}

public enum ReviewStatus
{
    Pending,
    Approved,
    Rejected,
    Modified
}

public enum ArtifactType
{
    Json,
    Markdown,
    Docx,
    Pdf,
    Table,
    Chart
}
