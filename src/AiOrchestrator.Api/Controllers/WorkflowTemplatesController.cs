using System.Text.Json;
using AiOrchestrator.Application;
using AiOrchestrator.Domain;
using Microsoft.AspNetCore.Mvc;

namespace AiOrchestrator.Api.Controllers;

[ApiController]
[Route("api/workflow-templates")]
public sealed class WorkflowTemplatesController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> ListAsync(
        [FromQuery] string? scenarioCode,
        ITemplateStore store,
        CancellationToken ct)
    {
        var templates = await store.ListWorkflowsAsync(scenarioCode, ct);
        return Ok(ApiResponse<object>.Ok(new { Items = templates.Select(ToSummary) }));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> GetAsync(
        Guid id,
        ITemplateStore store,
        CancellationToken ct)
    {
        var template = await store.FindWorkflowByIdAsync(id, ct);
        if (template is null)
            return NotFound(ApiResponse<object>.Fail("not_found", "Workflow template not found."));
        return Ok(ApiResponse<object>.Ok(ToDetail(template)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<object>>> CreateAsync(
        SaveWorkflowTemplateRequest request,
        ITemplateStore store,
        IWorkflowDefinitionLoader loader,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.ScenarioCode) || string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(ApiResponse<object>.Fail("invalid_request", "scenarioCode and name are required."));

        var validation = ValidateDag(request.Steps);
        if (validation is not null)
            return BadRequest(ApiResponse<object>.Fail("invalid_dag", validation));

        var template = new WorkflowTemplate
        {
            ScenarioCode = request.ScenarioCode,
            Name = request.Name,
            Version = string.IsNullOrWhiteSpace(request.Version) ? "1.0.0" : request.Version,
            InputSchemaJson = string.IsNullOrWhiteSpace(request.InputSchemaJson) ? "{}" : request.InputSchemaJson,
            CreatedBy = request.CreatedBy,
            Steps = MapSteps(request.Steps)
        };

        await store.AddWorkflowAsync(template, ct);
        await store.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { template.Id }));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateAsync(
        Guid id,
        SaveWorkflowTemplateRequest request,
        ITemplateStore store,
        CancellationToken ct)
    {
        var template = await store.FindWorkflowByIdAsync(id, ct);
        if (template is null)
            return NotFound(ApiResponse<object>.Fail("not_found", "Workflow template not found."));

        var validation = ValidateDag(request.Steps);
        if (validation is not null)
            return BadRequest(ApiResponse<object>.Fail("invalid_dag", validation));

        template.Name = request.Name;
        template.Version = string.IsNullOrWhiteSpace(request.Version) ? template.Version : request.Version;
        template.InputSchemaJson = string.IsNullOrWhiteSpace(request.InputSchemaJson) ? template.InputSchemaJson : request.InputSchemaJson;
        template.UpdatedAt = DateTimeOffset.UtcNow;

        // Remove old steps through EF tracker, then insert new ones directly via DbSet
        store.ClearWorkflowSteps(template);
        await store.AddWorkflowStepsAsync(MapSteps(request.Steps, id), ct);

        await store.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { template.Id }));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteAsync(
        Guid id,
        ITemplateStore store,
        CancellationToken ct)
    {
        var template = await store.FindWorkflowByIdAsync(id, ct);
        if (template is null)
            return NotFound(ApiResponse<object>.Fail("not_found", "Workflow template not found."));

        if (template.IsActive)
            return BadRequest(ApiResponse<object>.Fail("invalid_state", "Cannot delete an active workflow template. Deactivate it first."));

        store.ClearWorkflowSteps(template);
        store.RemoveWorkflow(template);
        await store.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { id }));
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<ActionResult<ApiResponse<object>>> ActivateAsync(
        Guid id,
        ITemplateStore store,
        CancellationToken ct)
    {
        var template = await store.FindWorkflowByIdAsync(id, ct);
        if (template is null)
            return NotFound(ApiResponse<object>.Fail("not_found", "Workflow template not found."));

        await store.DeactivateWorkflowsAsync(template.ScenarioCode, ct);
        template.IsActive = true;
        template.UpdatedAt = DateTimeOffset.UtcNow;
        await store.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { template.Id, IsActive = true }));
    }

    [HttpPost("clone-from-yaml")]
    public async Task<ActionResult<ApiResponse<object>>> CloneFromYamlAsync(
        [FromBody] CloneFromYamlRequest request,
        ITemplateStore store,
        IWorkflowDefinitionLoader loader,
        IAgentDefinitionLoader agentLoader,
        IPromptLoader promptLoader,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.ScenarioCode))
            return BadRequest(ApiResponse<object>.Fail("invalid_request", "scenarioCode is required."));

        WorkflowDefinition definition;
        try { definition = await loader.LoadAsync(request.ScenarioCode, ct); }
        catch (FileNotFoundException ex) { return NotFound(ApiResponse<object>.Fail("not_found", ex.Message)); }

        var steps = definition.Steps.Select((s, i) => new WorkflowStepTemplate
        {
            StepId = s.Id,
            Name = s.Name,
            Type = s.Type,
            SkillCode = s.Skill,
            AgentCode = s.Agent,
            DependsOnJson = JsonSerializer.Serialize(s.DependsOn),
            SortOrder = i
        }).ToList();

        var template = new WorkflowTemplate
        {
            ScenarioCode = request.ScenarioCode,
            Name = $"{definition.Name} (副本)",
            Version = definition.Version,
            InputSchemaJson = JsonSerializer.Serialize(definition.InputSchema),
            CreatedBy = request.CreatedBy,
            Steps = steps
        };

        await store.AddWorkflowAsync(template, ct);
        await store.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { template.Id }));
    }

    [HttpPost("validate-dag")]
    public ActionResult<ApiResponse<object>> ValidateDagEndpoint([FromBody] List<WorkflowStepTemplateDto> steps)
    {
        var error = ValidateDag(steps);
        return error is null
            ? Ok(ApiResponse<object>.Ok(new { Valid = true }))
            : Ok(ApiResponse<object>.Ok(new { Valid = false, Error = error }));
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string? ValidateDag(List<WorkflowStepTemplateDto> steps)
    {
        var ids = steps.Select(s => s.StepId).ToHashSet();
        foreach (var step in steps)
        {
            foreach (var dep in step.DependsOn)
            {
                if (!ids.Contains(dep))
                    return $"Step '{step.StepId}' depends on unknown step '{dep}'.";
            }
        }

        // DFS cycle detection
        var graph = steps.ToDictionary(s => s.StepId, s => s.DependsOn);
        var visited = new HashSet<string>();
        var inStack = new HashSet<string>();

        bool HasCycle(string node)
        {
            if (inStack.Contains(node)) return true;
            if (visited.Contains(node)) return false;
            visited.Add(node); inStack.Add(node);
            foreach (var dep in graph.GetValueOrDefault(node, []))
                if (HasCycle(dep)) return true;
            inStack.Remove(node);
            return false;
        }

        if (steps.Any(s => HasCycle(s.StepId)))
            return "Workflow steps contain a dependency cycle.";

        return null;
    }

    private static List<WorkflowStepTemplate> MapSteps(List<WorkflowStepTemplateDto> dtos, Guid? templateId = null) =>
        dtos.Select((dto, i) => new WorkflowStepTemplate
        {
            WorkflowTemplateId = templateId ?? Guid.Empty,
            StepId = dto.StepId,
            Name = dto.Name,
            Type = dto.Type,
            SkillCode = dto.SkillCode,
            AgentCode = dto.AgentCode,
            DependsOnJson = JsonSerializer.Serialize(dto.DependsOn),
            SortOrder = dto.SortOrder > 0 ? dto.SortOrder : i,
            DataSourceBindingsJson = dto.DataSourceBindingsJson ?? "{}"
        }).ToList();

    private static object ToSummary(WorkflowTemplate t) => new
    {
        t.Id, t.ScenarioCode, t.Name, t.Version, t.IsActive, t.CreatedAt, t.UpdatedAt,
        StepCount = t.Steps.Count
    };

    private static object ToDetail(WorkflowTemplate t) => new
    {
        t.Id, t.ScenarioCode, t.Name, t.Version, t.IsActive,
        t.InputSchemaJson, t.CreatedBy, t.CreatedAt, t.UpdatedAt,
        Steps = t.Steps.OrderBy(s => s.SortOrder).Select(s => new
        {
            s.Id, s.StepId, s.Name, s.Type, s.SkillCode, s.AgentCode,
            DependsOn = JsonSerializer.Deserialize<List<string>>(s.DependsOnJson),
            s.SortOrder, s.DataSourceBindingsJson
        })
    };
}

public sealed record CloneFromYamlRequest(string ScenarioCode, string? CreatedBy);
