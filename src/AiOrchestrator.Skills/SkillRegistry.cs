using System.Diagnostics;
using System.Text.Json.Nodes;
using AiOrchestrator.Application;
using AiOrchestrator.Domain;

namespace AiOrchestrator.Skills;

public sealed class SkillRegistry(IEnumerable<IAiSkill> skills) : ISkillRegistry
{
    private readonly Dictionary<string, IAiSkill> _skills = skills.ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);

    public IAiSkill GetRequired(string code)
    {
        return _skills.TryGetValue(code, out var skill)
            ? skill
            : throw new InvalidOperationException($"Skill '{code}' is not registered.");
    }

    public IReadOnlyCollection<SkillDefinition> List()
    {
        return _skills.Values
            .Select(x => new SkillDefinition(x.Code, x.Name, x.Description, x.InputSchema, x.OutputSchema))
            .ToList();
    }
}

public sealed class SkillExecutor(ISkillRegistry registry, IOrchestrationStore store) : ISkillExecutor
{
    public async Task<StepExecutionResult> ExecuteAsync(WorkflowExecutionContext context, WorkflowStepDefinition step, WorkflowStepRun stepRun, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(step.Skill))
        {
            return StepExecutionResult.Failed($"Step '{step.Id}' does not specify a skill.");
        }

        var skill = registry.GetRequired(step.Skill);
        var stopwatch = Stopwatch.StartNew();
        SkillResult result;

        try
        {
            result = await skill.ExecuteAsync(
                new SkillContext
                {
                    TaskId = context.TaskId,
                    WorkflowRunId = context.WorkflowRunId,
                    StepRunId = stepRun.Id,
                    ScenarioCode = context.ScenarioCode,
                    WorkflowContext = JsonSupport.CloneObject(context.Context)
                },
                JsonSupport.CloneObject(context.Context),
                ct);
        }
        catch (Exception ex)
        {
            result = new SkillResult { Success = false, ErrorMessage = ex.Message };
        }
        finally
        {
            stopwatch.Stop();
        }

        var output = result.Output is null ? new JsonObject() : JsonSupport.CloneNode(result.Output)!.AsObject();
        var evidenceIds = new JsonArray();
        foreach (var draft in result.EvidenceItems)
        {
            var evidence = new EvidenceItem
            {
                TaskId = context.TaskId,
                SourceType = draft.SourceType,
                SourceName = draft.SourceName,
                SourceUrl = draft.SourceUrl,
                FileId = draft.FileId,
                PageNo = draft.PageNo,
                SectionTitle = draft.SectionTitle,
                QuoteText = draft.QuoteText,
                ExtractedValueJson = JsonSupport.ToJson(draft.ExtractedValue),
                Confidence = draft.Confidence
            };
            evidenceIds.Add(evidence.Id.ToString());
            await store.AddEvidenceAsync(evidence, ct);
        }

        var artifactIds = new JsonArray();
        foreach (var draft in result.Artifacts)
        {
            var artifact = new Artifact
            {
                TaskId = context.TaskId,
                StepRunId = stepRun.Id,
                ArtifactType = ParseArtifactType(draft.ArtifactType),
                Name = draft.Name,
                ContentJson = JsonSupport.ToJson(draft.Content),
                FilePath = draft.FilePath
            };
            artifactIds.Add(artifact.Id.ToString());
            await store.AddArtifactAsync(artifact, ct);
        }

        if (evidenceIds.Count > 0)
        {
            output["created_evidence_ids"] = evidenceIds;
        }

        if (artifactIds.Count > 0)
        {
            output["created_artifact_ids"] = artifactIds;
        }

        var skillRun = new SkillRun
        {
            TaskId = context.TaskId,
            StepRunId = stepRun.Id,
            SkillCode = skill.Code,
            InputJson = JsonSupport.ToJson(context.Context),
            OutputJson = JsonSupport.ToJson(output),
            Status = result.Success ? "Succeeded" : "Failed",
            ErrorMessage = result.ErrorMessage,
            DurationMs = (int)stopwatch.ElapsedMilliseconds
        };
        await store.AddSkillRunAsync(skillRun, ct);

        await store.AddTraceAsync(new TraceEvent
        {
            TaskId = context.TaskId,
            WorkflowRunId = context.WorkflowRunId,
            StepRunId = stepRun.Id,
            EventType = "skill_called",
            Message = $"Skill {skill.Code} executed."
        }, ct);

        return result.Success ? StepExecutionResult.Succeeded(output) : StepExecutionResult.Failed(result.ErrorMessage ?? "Skill execution failed.");
    }

    private static ArtifactType ParseArtifactType(string value)
    {
        return Enum.TryParse<ArtifactType>(value, true, out var parsed) ? parsed : ArtifactType.Json;
    }
}
