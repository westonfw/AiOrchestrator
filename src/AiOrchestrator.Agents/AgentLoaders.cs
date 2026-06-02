using System.Text.Json;
using System.Text.Json.Nodes;
using AiOrchestrator.Application;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AiOrchestrator.Agents;

public sealed class AgentDefinitionLoader(ITemplateStore templateStore) : IAgentDefinitionLoader
{
    private readonly string _scenariosRoot = ScenarioFileSystem.ResolveScenariosRoot(null);
    private readonly IDeserializer _deserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public async Task<AgentDefinition> LoadAsync(string scenarioCode, string agentCode, CancellationToken ct = default)
    {
        // DB-first: check for an agent template
        var template = await templateStore.FindAgentAsync(scenarioCode, agentCode, ct);
        if (template is not null)
            return MapFromTemplate(template);

        // Fallback: load from agents.yaml
        return await LoadFromYamlAsync(scenarioCode, agentCode, ct);
    }

    private static AgentDefinition MapFromTemplate(Domain.AgentTemplate t) => new()
    {
        Code = t.AgentCode,
        Name = t.Name,
        Description = t.Description,
        Model = t.Model,
        Temperature = t.Temperature,
        SystemPromptFile = string.Empty,
        OutputSchema = string.Empty,
        AllowedSkills = JsonSerializer.Deserialize<List<string>>(t.AllowedSkillsJson) ?? new(),
        // Inline fields — AgentExecutor will use these directly
        SystemPromptText = t.SystemPrompt,
        OutputSchemaJsonText = t.OutputSchemaJson,
        AllowedDataSources = JsonSerializer.Deserialize<List<string>>(t.AllowedDataSourcesJson) ?? new(),
        MaxToolCalls = t.MaxToolCalls
    };

    private async Task<AgentDefinition> LoadFromYamlAsync(string scenarioCode, string agentCode, CancellationToken ct)
    {
        var agentsPath = Path.Combine(_scenariosRoot, scenarioCode, "agents.yaml");
        if (!File.Exists(agentsPath))
            throw new FileNotFoundException($"Agent definition file not found for scenario '{scenarioCode}'.", agentsPath);

        var yaml = await File.ReadAllTextAsync(agentsPath, ct);
        var file = _deserializer.Deserialize<AgentDefinitionFile>(yaml)
            ?? throw new InvalidOperationException($"Agent definition file '{agentsPath}' is empty.");
        return file.Agents.FirstOrDefault(x => string.Equals(x.Code, agentCode, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Agent '{agentCode}' is not defined in scenario '{scenarioCode}'.");
    }
}

public sealed class PromptLoader : IPromptLoader
{
    private readonly string _scenariosRoot;

    public PromptLoader() : this(null) { }

    public PromptLoader(string? scenariosRoot)
    {
        _scenariosRoot = ScenarioFileSystem.ResolveScenariosRoot(scenariosRoot);
    }

    public Task<string> LoadAsync(string scenarioCode, string relativePromptPath, CancellationToken ct = default)
    {
        var promptPath = Path.Combine(_scenariosRoot, scenarioCode, relativePromptPath);
        if (!File.Exists(promptPath))
            throw new FileNotFoundException($"Prompt file not found: {relativePromptPath}.", promptPath);
        return File.ReadAllTextAsync(promptPath, ct);
    }
}

public sealed class SchemaLoader
{
    private readonly string _scenariosRoot;

    public SchemaLoader() : this(null) { }

    public SchemaLoader(string? scenariosRoot)
    {
        _scenariosRoot = ScenarioFileSystem.ResolveScenariosRoot(scenariosRoot);
    }

    public async Task<JsonNode?> LoadAsync(string scenarioCode, string relativeSchemaPath, CancellationToken ct)
    {
        var schemaPath = Path.Combine(_scenariosRoot, scenarioCode, relativeSchemaPath);
        if (!File.Exists(schemaPath))
            throw new FileNotFoundException($"Schema file not found: {relativeSchemaPath}.", schemaPath);
        return JsonNode.Parse(await File.ReadAllTextAsync(schemaPath, ct));
    }
}
