using System.Text.Json;
using AiOrchestrator.Application;
using AiOrchestrator.Domain;
using Microsoft.AspNetCore.Mvc;

namespace AiOrchestrator.Api.Controllers;

[ApiController]
[Route("api/agent-templates")]
public sealed class AgentTemplatesController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> ListAsync(
        [FromQuery] string? scenarioCode,
        ITemplateStore store,
        CancellationToken ct)
    {
        var agents = await store.ListAgentsAsync(scenarioCode, ct);
        return Ok(ApiResponse<object>.Ok(new { Items = agents.Select(ToSummary) }));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> GetAsync(
        Guid id,
        ITemplateStore store,
        CancellationToken ct)
    {
        var agent = await store.FindAgentByIdAsync(id, ct);
        if (agent is null)
            return NotFound(ApiResponse<object>.Fail("not_found", "Agent template not found."));
        return Ok(ApiResponse<object>.Ok(ToDetail(agent)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<object>>> CreateAsync(
        SaveAgentTemplateRequest request,
        ITemplateStore store,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.ScenarioCode) || string.IsNullOrWhiteSpace(request.AgentCode))
            return BadRequest(ApiResponse<object>.Fail("invalid_request", "scenarioCode and agentCode are required."));

        var existing = await store.FindAgentAsync(request.ScenarioCode, request.AgentCode, ct);
        if (existing is not null)
            return Conflict(ApiResponse<object>.Fail("conflict", $"Agent '{request.AgentCode}' already exists in scenario '{request.ScenarioCode}'."));

        var agent = MapFromRequest(request);
        await store.AddAgentAsync(agent, ct);
        await store.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { agent.Id }));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateAsync(
        Guid id,
        SaveAgentTemplateRequest request,
        ITemplateStore store,
        CancellationToken ct)
    {
        var agent = await store.FindAgentByIdAsync(id, ct);
        if (agent is null)
            return NotFound(ApiResponse<object>.Fail("not_found", "Agent template not found."));

        agent.Name = request.Name;
        agent.Description = request.Description;
        agent.Model = request.Model;
        agent.Temperature = request.Temperature;
        agent.SystemPrompt = request.SystemPrompt;
        agent.OutputSchemaJson = string.IsNullOrWhiteSpace(request.OutputSchemaJson) ? "{}" : request.OutputSchemaJson;
        agent.AllowedSkillsJson = JsonSerializer.Serialize(request.AllowedSkills ?? new());
        agent.AllowedDataSourcesJson = JsonSerializer.Serialize(request.AllowedDataSources ?? new());
        agent.MaxToolCalls = request.MaxToolCalls > 0 ? request.MaxToolCalls : 10;
        agent.UpdatedAt = DateTimeOffset.UtcNow;

        await store.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { agent.Id }));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteAsync(
        Guid id,
        ITemplateStore store,
        CancellationToken ct)
    {
        var agent = await store.FindAgentByIdAsync(id, ct);
        if (agent is null)
            return NotFound(ApiResponse<object>.Fail("not_found", "Agent template not found."));

        store.RemoveAgent(agent);
        await store.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { id }));
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static AgentTemplate MapFromRequest(SaveAgentTemplateRequest r) => new()
    {
        ScenarioCode = r.ScenarioCode,
        AgentCode = r.AgentCode,
        Name = r.Name,
        Description = r.Description,
        Model = string.IsNullOrWhiteSpace(r.Model) ? "default" : r.Model,
        Temperature = r.Temperature,
        SystemPrompt = r.SystemPrompt,
        OutputSchemaJson = string.IsNullOrWhiteSpace(r.OutputSchemaJson) ? "{}" : r.OutputSchemaJson,
        AllowedSkillsJson = JsonSerializer.Serialize(r.AllowedSkills ?? new()),
        AllowedDataSourcesJson = JsonSerializer.Serialize(r.AllowedDataSources ?? new()),
        MaxToolCalls = r.MaxToolCalls > 0 ? r.MaxToolCalls : 10
    };

    private static object ToSummary(AgentTemplate a) => new
    {
        a.Id, a.ScenarioCode, a.AgentCode, a.Name, a.Description,
        a.Model, a.Temperature, a.CreatedAt, a.UpdatedAt
    };

    private static object ToDetail(AgentTemplate a) => new
    {
        a.Id, a.ScenarioCode, a.AgentCode, a.Name, a.Description,
        a.Model, a.Temperature, a.SystemPrompt, a.OutputSchemaJson,
        AllowedSkills = JsonSerializer.Deserialize<List<string>>(a.AllowedSkillsJson),
        AllowedDataSources = JsonSerializer.Deserialize<List<string>>(a.AllowedDataSourcesJson),
        a.MaxToolCalls, a.CreatedBy, a.CreatedAt, a.UpdatedAt
    };
}
