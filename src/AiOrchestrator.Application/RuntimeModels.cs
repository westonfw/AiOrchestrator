using System.Text.Json.Nodes;

namespace AiOrchestrator.Application;

public sealed class WorkflowExecutionContext
{
    public Guid TaskId { get; set; }
    public Guid WorkflowRunId { get; set; }
    public string ScenarioCode { get; set; } = string.Empty;
    public JsonObject Input { get; set; } = new();
    public JsonObject Context { get; set; } = new();
    public Dictionary<string, JsonNode?> StepOutputs { get; set; } = new();
}

public sealed class StepExecutionResult
{
    public bool Success { get; init; }
    public bool WaitingReview { get; init; }
    public JsonNode? Output { get; init; }
    public string? ErrorMessage { get; init; }

    public static StepExecutionResult Succeeded(JsonNode? output) => new() { Success = true, Output = output };
    public static StepExecutionResult Failed(string error) => new() { Success = false, ErrorMessage = error };
    public static StepExecutionResult Waiting(JsonNode? output) => new() { Success = true, WaitingReview = true, Output = output };
}

public sealed class SkillContext
{
    public Guid TaskId { get; init; }
    public Guid WorkflowRunId { get; init; }
    public Guid StepRunId { get; init; }
    public string ScenarioCode { get; init; } = string.Empty;
    public JsonObject WorkflowContext { get; init; } = new();
}

public sealed class SkillResult
{
    public bool Success { get; set; }
    public JsonNode? Output { get; set; }
    public List<EvidenceDraft> EvidenceItems { get; set; } = new();
    public List<ArtifactDraft> Artifacts { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

public sealed class EvidenceDraft
{
    public string SourceType { get; set; } = "uploaded_text";
    public string? SourceName { get; set; }
    public string? SourceUrl { get; set; }
    public Guid? FileId { get; set; }
    public int? PageNo { get; set; }
    public string? SectionTitle { get; set; }
    public string? QuoteText { get; set; }
    public JsonNode? ExtractedValue { get; set; }
    public decimal Confidence { get; set; } = 0.8m;
}

public sealed class ArtifactDraft
{
    public string ArtifactType { get; set; } = "json";
    public string Name { get; set; } = string.Empty;
    public JsonNode? Content { get; set; }
    public string? FilePath { get; set; }
}

public sealed class LlmRequest
{
    public string AgentCode { get; set; } = string.Empty;
    public string Model { get; set; } = "mock";
    public decimal Temperature { get; set; }
    public List<LlmMessage> Messages { get; set; } = new();
    public JsonNode? OutputSchema { get; set; }
    public JsonNode? Input { get; set; }
}

public sealed record LlmMessage(string Role, string Content);

public sealed class LlmResult
{
    public string RawOutput { get; set; } = "{}";
    public JsonNode? JsonOutput { get; set; }
    public string TokenUsageJson { get; set; } = "{}";
}

public sealed record SchemaValidationResult(bool IsValid, string? ErrorMessage);
