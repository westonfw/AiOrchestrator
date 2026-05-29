namespace AiOrchestrator.Domain;

public sealed class AiTask
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ScenarioCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string InputJson { get; set; } = "{}";
    public TaskStatus Status { get; set; } = TaskStatus.Created;
    public string? CurrentStep { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class WorkflowRun
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TaskId { get; set; }
    public AiTask? Task { get; set; }
    public string WorkflowCode { get; set; } = string.Empty;
    public string WorkflowVersion { get; set; } = string.Empty;
    public WorkflowRunStatus Status { get; set; } = WorkflowRunStatus.Created;
    public string ContextJson { get; set; } = "{}";
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class WorkflowStepRun
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkflowRunId { get; set; }
    public WorkflowRun? WorkflowRun { get; set; }
    public Guid TaskId { get; set; }
    public AiTask? Task { get; set; }
    public string StepId { get; set; } = string.Empty;
    public string? StepName { get; set; }
    public string StepType { get; set; } = string.Empty;
    public StepRunStatus Status { get; set; } = StepRunStatus.Pending;
    public string? InputJson { get; set; }
    public string? OutputJson { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class AgentRun
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TaskId { get; set; }
    public AiTask? Task { get; set; }
    public Guid StepRunId { get; set; }
    public WorkflowStepRun? StepRun { get; set; }
    public string AgentCode { get; set; } = string.Empty;
    public string? Model { get; set; }
    public string? PromptText { get; set; }
    public string? InputJson { get; set; }
    public string? RawOutput { get; set; }
    public string? OutputJson { get; set; }
    public bool SchemaValid { get; set; }
    public string? TokenUsageJson { get; set; }
    public string? Status { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class SkillRun
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TaskId { get; set; }
    public AiTask? Task { get; set; }
    public Guid StepRunId { get; set; }
    public WorkflowStepRun? StepRun { get; set; }
    public string SkillCode { get; set; } = string.Empty;
    public string? InputJson { get; set; }
    public string? OutputJson { get; set; }
    public string? Status { get; set; }
    public string? ErrorMessage { get; set; }
    public int DurationMs { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class EvidenceItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TaskId { get; set; }
    public AiTask? Task { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public string? SourceName { get; set; }
    public string? SourceUrl { get; set; }
    public Guid? FileId { get; set; }
    public int? PageNo { get; set; }
    public string? SectionTitle { get; set; }
    public string? QuoteText { get; set; }
    public string? ExtractedValueJson { get; set; }
    public decimal Confidence { get; set; }
    public bool Verified { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class Artifact
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TaskId { get; set; }
    public AiTask? Task { get; set; }
    public Guid? StepRunId { get; set; }
    public WorkflowStepRun? StepRun { get; set; }
    public ArtifactType ArtifactType { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ContentJson { get; set; }
    public string? FilePath { get; set; }
    public int Version { get; set; } = 1;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class ReviewItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TaskId { get; set; }
    public AiTask? Task { get; set; }
    public Guid StepRunId { get; set; }
    public WorkflowStepRun? StepRun { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ContentJson { get; set; } = "{}";
    public ReviewStatus Status { get; set; } = ReviewStatus.Pending;
    public string? Reviewer { get; set; }
    public string? ReviewComment { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class UploadedFile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? TaskId { get; set; }
    public AiTask? Task { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFilePath { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long FileSize { get; set; }
    public string? ExtractedText { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class TraceEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? TaskId { get; set; }
    public AiTask? Task { get; set; }
    public Guid? WorkflowRunId { get; set; }
    public WorkflowRun? WorkflowRun { get; set; }
    public Guid? StepRunId { get; set; }
    public WorkflowStepRun? StepRun { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string? PayloadJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
