using System.Text.Json.Nodes;

namespace AiOrchestrator.Application;

public sealed record ApiResponse<T>(bool Success, T? Data, string? ErrorCode = null, string? ErrorMessage = null)
{
    public static ApiResponse<T> Ok(T data) => new(true, data);
    public static ApiResponse<T> Fail(string errorCode, string errorMessage) => new(false, default, errorCode, errorMessage);
}

public sealed record CreateTaskRequest(string ScenarioCode, string Title, JsonObject Input, string? CreatedBy);

public sealed record ReviewActionRequest(string? Comment, JsonObject? ModifiedContent);

// ── Template API models ──────────────────────────────────────────────────────

public sealed record WorkflowStepTemplateDto(
    string StepId,
    string Name,
    string Type,
    string? SkillCode,
    string? AgentCode,
    List<string> DependsOn,
    int SortOrder,
    string? DataSourceBindingsJson);

public sealed record SaveWorkflowTemplateRequest(
    string ScenarioCode,
    string Name,
    string Version,
    string InputSchemaJson,
    List<WorkflowStepTemplateDto> Steps,
    string? CreatedBy);

public sealed record SaveAgentTemplateRequest(
    string ScenarioCode,
    string AgentCode,
    string Name,
    string Description,
    string Model,
    decimal Temperature,
    string SystemPrompt,
    string OutputSchemaJson,
    List<string> AllowedSkills,
    List<string> AllowedDataSources,
    int MaxToolCalls);
