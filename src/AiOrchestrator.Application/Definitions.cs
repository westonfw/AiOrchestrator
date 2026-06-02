using System.Text.Json.Nodes;
using YamlDotNet.Serialization;

namespace AiOrchestrator.Application;

public sealed class WorkflowDefinition
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Scenario { get; set; } = string.Empty;
    public Dictionary<string, string> InputSchema { get; set; } = new();
    public List<WorkflowStepDefinition> Steps { get; set; } = new();
}

public sealed class WorkflowStepDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Skill { get; set; }
    public string? Agent { get; set; }

    [YamlMember(Alias = "depends_on")]
    public List<string> DependsOn { get; set; } = new();

    [YamlMember(Alias = "output_schema")]
    public string? OutputSchema { get; set; }
}

public sealed class AgentDefinition
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Model { get; set; } = "mock";
    public decimal Temperature { get; set; } = 0.2m;

    [YamlMember(Alias = "system_prompt_file")]
    public string SystemPromptFile { get; set; } = string.Empty;

    [YamlMember(Alias = "output_schema")]
    public string OutputSchema { get; set; } = string.Empty;

    [YamlMember(Alias = "allowed_skills")]
    public List<string> AllowedSkills { get; set; } = new();

    [YamlMember(Alias = "max_retries")]
    public int MaxRetries { get; set; } = 0;

    // Populated when loaded from DB (overrides file-based loading)
    [YamlIgnore] public string? SystemPromptText { get; set; }
    [YamlIgnore] public string? OutputSchemaJsonText { get; set; }
    [YamlIgnore] public List<string> AllowedDataSources { get; set; } = new();
    [YamlIgnore] public int MaxToolCalls { get; set; } = 10;
}

public sealed class AgentDefinitionFile
{
    public List<AgentDefinition> Agents { get; set; } = new();
}

public sealed record SkillDefinition(
    string Code,
    string Name,
    string Description,
    JsonNode InputSchema,
    JsonNode OutputSchema,
    bool IsSensitive = false,
    bool RequireReview = false);
