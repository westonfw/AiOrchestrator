using System.Text.Json;
using System.Text.Json.Nodes;
using AiOrchestrator.Application;
using AiOrchestrator.Domain;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AiOrchestrator.Workflow;

public sealed class WorkflowDefinitionLoader(ITemplateStore templateStore) : IWorkflowDefinitionLoader
{
    private readonly string _scenariosRoot = ScenarioFileSystem.ResolveScenariosRoot(null);
    private readonly IDeserializer _deserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public async Task<WorkflowDefinition> LoadAsync(string scenarioCode, CancellationToken ct = default)
    {
        // DB-first: check for an active workflow template
        var template = await templateStore.FindActiveWorkflowAsync(scenarioCode, ct);
        if (template is not null)
            return MapFromTemplate(template);

        // Fallback: load from YAML file
        return await LoadFromYamlAsync(scenarioCode, ct);
    }

    private static WorkflowDefinition MapFromTemplate(WorkflowTemplate t)
    {
        var steps = t.Steps
            .OrderBy(s => s.SortOrder)
            .Select(s => new WorkflowStepDefinition
            {
                Id = s.StepId,
                Name = s.Name,
                Type = s.Type,
                Skill = s.SkillCode,
                Agent = s.AgentCode,
                DependsOn = JsonSerializer.Deserialize<List<string>>(s.DependsOnJson) ?? new()
            })
            .ToList();

        var inputSchema = new Dictionary<string, string>();
        try
        {
            inputSchema = JsonSerializer.Deserialize<Dictionary<string, string>>(t.InputSchemaJson)
                ?? new Dictionary<string, string>();
        }
        catch { /* ignore malformed input schema */ }

        return new WorkflowDefinition
        {
            Code = t.Id.ToString("N"),
            Name = t.Name,
            Version = t.Version,
            Scenario = t.ScenarioCode,
            InputSchema = inputSchema,
            Steps = steps
        };
    }

    private async Task<WorkflowDefinition> LoadFromYamlAsync(string scenarioCode, CancellationToken ct)
    {
        var workflowPath = Path.Combine(_scenariosRoot, scenarioCode, "workflow.yaml");
        if (!File.Exists(workflowPath))
            throw new FileNotFoundException($"Workflow definition not found for scenario '{scenarioCode}'.", workflowPath);

        var yaml = await File.ReadAllTextAsync(workflowPath, ct);
        var definition = _deserializer.Deserialize<WorkflowDefinition>(yaml)
            ?? throw new InvalidOperationException($"Workflow definition '{workflowPath}' is empty.");

        definition.Scenario = string.IsNullOrWhiteSpace(definition.Scenario) ? scenarioCode : definition.Scenario;
        return definition;
    }
}
