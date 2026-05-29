using AiOrchestrator.Application;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AiOrchestrator.Workflow;

public sealed class WorkflowDefinitionLoader : IWorkflowDefinitionLoader
{
    private readonly string _scenariosRoot;
    private readonly IDeserializer _deserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public WorkflowDefinitionLoader() : this(null)
    {
    }

    public WorkflowDefinitionLoader(string? scenariosRoot)
    {
        _scenariosRoot = ScenarioFileSystem.ResolveScenariosRoot(scenariosRoot);
    }

    public async Task<WorkflowDefinition> LoadAsync(string scenarioCode, CancellationToken ct = default)
    {
        var workflowPath = Path.Combine(_scenariosRoot, scenarioCode, "workflow.yaml");
        if (!File.Exists(workflowPath))
        {
            throw new FileNotFoundException($"Workflow definition not found for scenario '{scenarioCode}'.", workflowPath);
        }

        var yaml = await File.ReadAllTextAsync(workflowPath, ct);
        var definition = _deserializer.Deserialize<WorkflowDefinition>(yaml)
            ?? throw new InvalidOperationException($"Workflow definition '{workflowPath}' is empty.");

        definition.Scenario = string.IsNullOrWhiteSpace(definition.Scenario) ? scenarioCode : definition.Scenario;
        return definition;
    }
}
