using System.Text.Json.Nodes;
using AiOrchestrator.Application;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AiOrchestrator.Agents;

public sealed class AgentDefinitionLoader : IAgentDefinitionLoader
{
    private readonly string _scenariosRoot;
    private readonly IDeserializer _deserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public AgentDefinitionLoader() : this(null)
    {
    }

    public AgentDefinitionLoader(string? scenariosRoot)
    {
        _scenariosRoot = ScenarioFileSystem.ResolveScenariosRoot(scenariosRoot);
    }

    public async Task<AgentDefinition> LoadAsync(string scenarioCode, string agentCode, CancellationToken ct = default)
    {
        var agentsPath = Path.Combine(_scenariosRoot, scenarioCode, "agents.yaml");
        if (!File.Exists(agentsPath))
        {
            throw new FileNotFoundException($"Agent definition file not found for scenario '{scenarioCode}'.", agentsPath);
        }

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

    public PromptLoader() : this(null)
    {
    }

    public PromptLoader(string? scenariosRoot)
    {
        _scenariosRoot = ScenarioFileSystem.ResolveScenariosRoot(scenariosRoot);
    }

    public Task<string> LoadAsync(string scenarioCode, string relativePromptPath, CancellationToken ct = default)
    {
        var promptPath = Path.Combine(_scenariosRoot, scenarioCode, relativePromptPath);
        if (!File.Exists(promptPath))
        {
            throw new FileNotFoundException($"Prompt file not found: {relativePromptPath}.", promptPath);
        }

        return File.ReadAllTextAsync(promptPath, ct);
    }
}

public sealed class SchemaLoader
{
    private readonly string _scenariosRoot;

    public SchemaLoader() : this(null)
    {
    }

    public SchemaLoader(string? scenariosRoot)
    {
        _scenariosRoot = ScenarioFileSystem.ResolveScenariosRoot(scenariosRoot);
    }

    public async Task<JsonNode?> LoadAsync(string scenarioCode, string relativeSchemaPath, CancellationToken ct)
    {
        var schemaPath = Path.Combine(_scenariosRoot, scenarioCode, relativeSchemaPath);
        if (!File.Exists(schemaPath))
        {
            throw new FileNotFoundException($"Schema file not found: {relativeSchemaPath}.", schemaPath);
        }

        return JsonNode.Parse(await File.ReadAllTextAsync(schemaPath, ct));
    }
}
