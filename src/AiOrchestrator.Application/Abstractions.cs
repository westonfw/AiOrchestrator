using System.Text.Json.Nodes;
using AiOrchestrator.Domain;

namespace AiOrchestrator.Application;

public interface IWorkflowExecutor
{
    Task<Guid> StartAsync(Guid taskId, CancellationToken ct = default);
    Task ContinueAsync(Guid workflowRunId, CancellationToken ct = default);
}

public interface IWorkflowDefinitionLoader
{
    Task<WorkflowDefinition> LoadAsync(string scenarioCode, CancellationToken ct = default);
}

public interface ISkillRegistry
{
    IAiSkill GetRequired(string code);
    IReadOnlyCollection<SkillDefinition> List();
}

public interface ISkillExecutor
{
    Task<StepExecutionResult> ExecuteAsync(WorkflowExecutionContext context, WorkflowStepDefinition step, WorkflowStepRun stepRun, CancellationToken ct);
}

public interface IAiSkill
{
    string Code { get; }
    string Name { get; }
    string Description { get; }
    JsonNode InputSchema { get; }
    JsonNode OutputSchema { get; }
    Task<SkillResult> ExecuteAsync(SkillContext context, JsonNode input, CancellationToken ct);
}

public interface IAgentDefinitionLoader
{
    Task<AgentDefinition> LoadAsync(string scenarioCode, string agentCode, CancellationToken ct = default);
}

public interface IPromptLoader
{
    Task<string> LoadAsync(string scenarioCode, string relativePromptPath, CancellationToken ct = default);
}

public interface IAgentExecutor
{
    Task<StepExecutionResult> ExecuteAsync(WorkflowExecutionContext context, WorkflowStepDefinition step, WorkflowStepRun stepRun, CancellationToken ct);
}

public interface ILlmProvider
{
    Task<LlmResult> GenerateJsonAsync(LlmRequest request, CancellationToken ct);
}

public interface IJsonSchemaValidator
{
    SchemaValidationResult Validate(JsonNode? schema, JsonNode? payload);
}

public interface ITaskQueue
{
    Task EnqueueStartWorkflowAsync(Guid taskId, CancellationToken ct = default);
    Task EnqueueContinueWorkflowAsync(Guid workflowRunId, CancellationToken ct = default);
}

public interface ITemplateStore
{
    // WorkflowTemplate
    Task<WorkflowTemplate?> FindActiveWorkflowAsync(string scenarioCode, CancellationToken ct = default);
    Task<WorkflowTemplate?> FindWorkflowByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<WorkflowTemplate>> ListWorkflowsAsync(string? scenarioCode, CancellationToken ct = default);
    Task AddWorkflowAsync(WorkflowTemplate template, CancellationToken ct = default);
    void ClearWorkflowSteps(WorkflowTemplate template);
    Task AddWorkflowStepsAsync(IEnumerable<WorkflowStepTemplate> steps, CancellationToken ct = default);
    void RemoveWorkflow(WorkflowTemplate template);
    Task DeactivateWorkflowsAsync(string scenarioCode, CancellationToken ct = default);

    // AgentTemplate
    Task<AgentTemplate?> FindAgentAsync(string scenarioCode, string agentCode, CancellationToken ct = default);
    Task<AgentTemplate?> FindAgentByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<AgentTemplate>> ListAgentsAsync(string? scenarioCode, CancellationToken ct = default);
    Task AddAgentAsync(AgentTemplate template, CancellationToken ct = default);
    void RemoveAgent(AgentTemplate template);

    Task SaveChangesAsync(CancellationToken ct = default);
}

public interface IOrchestrationStore
{
    Task<AiTask?> FindTaskAsync(Guid taskId, CancellationToken ct = default);
    Task<List<AiTask>> ListTasksAsync(string? scenarioCode, string? status, int pageIndex, int pageSize, CancellationToken ct = default);
    Task AddTaskAsync(AiTask task, CancellationToken ct = default);
    Task<WorkflowRun?> FindWorkflowRunAsync(Guid workflowRunId, CancellationToken ct = default);
    Task<WorkflowRun?> FindLatestWorkflowRunAsync(Guid taskId, CancellationToken ct = default);
    Task AddWorkflowRunAsync(WorkflowRun workflowRun, CancellationToken ct = default);
    Task<WorkflowStepRun?> FindStepRunAsync(Guid stepRunId, CancellationToken ct = default);
    Task<List<WorkflowStepRun>> ListStepRunsAsync(Guid workflowRunId, CancellationToken ct = default);
    Task AddStepRunAsync(WorkflowStepRun stepRun, CancellationToken ct = default);
    Task AddSkillRunAsync(SkillRun skillRun, CancellationToken ct = default);
    Task AddAgentRunAsync(AgentRun agentRun, CancellationToken ct = default);
    Task AddEvidenceAsync(EvidenceItem evidenceItem, CancellationToken ct = default);
    Task AddArtifactAsync(Artifact artifact, CancellationToken ct = default);
    Task AddReviewAsync(ReviewItem reviewItem, CancellationToken ct = default);
    Task<ReviewItem?> FindReviewAsync(Guid reviewId, CancellationToken ct = default);
    Task<List<ReviewItem>> ListReviewsAsync(Guid? taskId, ReviewStatus? status, CancellationToken ct = default);
    Task<List<Artifact>> ListArtifactsAsync(Guid taskId, CancellationToken ct = default);
    Task<Artifact?> FindArtifactAsync(Guid artifactId, CancellationToken ct = default);
    Task<List<EvidenceItem>> ListEvidenceAsync(Guid taskId, CancellationToken ct = default);
    Task<EvidenceItem?> FindEvidenceAsync(Guid evidenceId, CancellationToken ct = default);
    Task<List<TraceEvent>> ListTraceAsync(Guid taskId, CancellationToken ct = default);
    Task AddTraceAsync(TraceEvent traceEvent, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
