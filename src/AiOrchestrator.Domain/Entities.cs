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

// ── Template entities ──────────────────────────────────────────────────────

public sealed class WorkflowTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ScenarioCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public bool IsActive { get; set; }
    public string InputSchemaJson { get; set; } = "{}";
    public string? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<WorkflowStepTemplate> Steps { get; set; } = new();
}

public sealed class WorkflowStepTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkflowTemplateId { get; set; }
    public WorkflowTemplate WorkflowTemplate { get; set; } = null!;
    public string StepId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // skill | agent | review
    public string? SkillCode { get; set; }
    public string? AgentCode { get; set; }
    public string DependsOnJson { get; set; } = "[]";
    public int SortOrder { get; set; }
    // JSON object for future data source bindings: {"param": "datasource_code"}
    public string DataSourceBindingsJson { get; set; } = "{}";
}

public sealed class AgentTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ScenarioCode { get; set; } = string.Empty;
    public string AgentCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Model { get; set; } = "default";
    public decimal Temperature { get; set; } = 0.2m;
    public string SystemPrompt { get; set; } = string.Empty;
    public string OutputSchemaJson { get; set; } = "{}";
    // JSON arrays — AllowedDataSources reserved for future data source integration
    public string AllowedSkillsJson { get; set; } = "[]";
    public string AllowedDataSourcesJson { get; set; } = "[]";
    public int MaxToolCalls { get; set; } = 10;
    public string? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

// ── End Template entities ───────────────────────────────────────────────────

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
