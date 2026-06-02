using System.Text.Json.Nodes;
using AiOrchestrator.Application;
using AiOrchestrator.Domain;

namespace AiOrchestrator.Agents;

public sealed class AgentExecutor(
    IAgentDefinitionLoader agentDefinitionLoader,
    IPromptLoader promptLoader,
    SchemaLoader schemaLoader,
    ILlmProvider llmProvider,
    IJsonSchemaValidator schemaValidator,
    IOrchestrationStore store) : IAgentExecutor
{
    public async Task<StepExecutionResult> ExecuteAsync(WorkflowExecutionContext context, WorkflowStepDefinition step, WorkflowStepRun stepRun, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(step.Agent))
        {
            return StepExecutionResult.Failed($"Step '{step.Id}' does not specify an agent.");
        }

        var agent = await agentDefinitionLoader.LoadAsync(context.ScenarioCode, step.Agent, ct);

        // Use inline text from DB template; fall back to file-based loading for built-in agents
        var prompt = !string.IsNullOrWhiteSpace(agent.SystemPromptText)
            ? agent.SystemPromptText
            : await promptLoader.LoadAsync(context.ScenarioCode, agent.SystemPromptFile, ct);

        JsonNode? schema;
        if (!string.IsNullOrWhiteSpace(agent.OutputSchemaJsonText))
        {
            schema = System.Text.Json.Nodes.JsonNode.Parse(agent.OutputSchemaJsonText);
        }
        else
        {
            var schemaPath = step.OutputSchema ?? agent.OutputSchema;
            schema = string.IsNullOrWhiteSpace(schemaPath)
                ? null
                : await schemaLoader.LoadAsync(context.ScenarioCode, schemaPath, ct);
        }
        var input = JsonSupport.CloneObject(context.Context);

        var agentRun = new AgentRun
        {
            TaskId = context.TaskId,
            StepRunId = stepRun.Id,
            AgentCode = agent.Code,
            Model = agent.Model,
            PromptText = prompt,
            InputJson = JsonSupport.ToJson(input),
            Status = "Running"
        };
        await store.AddAgentRunAsync(agentRun, ct);

        try
        {
            var result = await llmProvider.GenerateJsonAsync(new LlmRequest
            {
                AgentCode = agent.Code,
                Model = agent.Model,
                Temperature = agent.Temperature,
                OutputSchema = schema,
                Input = input,
                Messages =
                {
                    new LlmMessage("system", prompt),
                    new LlmMessage("user", "Workflow context JSON:\n" + input.ToJsonString(JsonSupport.SerializerOptions))
                }
            }, ct);

            var output = result.JsonOutput ?? JsonSupport.ParseNode(result.RawOutput);
            var validation = schemaValidator.Validate(schema, output);

            agentRun.RawOutput = result.RawOutput;
            agentRun.OutputJson = JsonSupport.ToJson(output);
            agentRun.TokenUsageJson = result.TokenUsageJson;
            agentRun.SchemaValid = validation.IsValid;
            agentRun.Status = validation.IsValid ? "Succeeded" : "Failed";
            agentRun.ErrorMessage = validation.ErrorMessage;

            await store.AddTraceAsync(new TraceEvent
            {
                TaskId = context.TaskId,
                WorkflowRunId = context.WorkflowRunId,
                StepRunId = stepRun.Id,
                EventType = "agent_called",
                Message = $"Agent {agent.Code} executed."
            }, ct);

            return validation.IsValid
                ? StepExecutionResult.Succeeded(output)
                : StepExecutionResult.Failed(validation.ErrorMessage ?? "Agent output schema validation failed.");
        }
        catch (Exception ex)
        {
            agentRun.Status = "Failed";
            agentRun.ErrorMessage = ex.Message;
            return StepExecutionResult.Failed(ex.Message);
        }
    }
}
