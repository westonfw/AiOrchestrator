using AiOrchestrator.Application;
using AiOrchestrator.Domain;
using Microsoft.EntityFrameworkCore;

namespace AiOrchestrator.Infrastructure;

public sealed class EfTemplateStore(AiOrchestratorDbContext db) : ITemplateStore
{
    public Task<WorkflowTemplate?> FindActiveWorkflowAsync(string scenarioCode, CancellationToken ct = default) =>
        db.WorkflowTemplates
            .Include(x => x.Steps)
            .FirstOrDefaultAsync(x => x.ScenarioCode == scenarioCode && x.IsActive, ct);

    public Task<WorkflowTemplate?> FindWorkflowByIdAsync(Guid id, CancellationToken ct = default) =>
        db.WorkflowTemplates
            .Include(x => x.Steps)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<List<WorkflowTemplate>> ListWorkflowsAsync(string? scenarioCode, CancellationToken ct = default)
    {
        var query = db.WorkflowTemplates.Include(x => x.Steps).AsQueryable();
        if (!string.IsNullOrWhiteSpace(scenarioCode))
            query = query.Where(x => x.ScenarioCode == scenarioCode);
        return query.OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
    }

    public async Task AddWorkflowAsync(WorkflowTemplate template, CancellationToken ct = default) =>
        await db.WorkflowTemplates.AddAsync(template, ct);

    public void ClearWorkflowSteps(WorkflowTemplate template)
    {
        db.WorkflowStepTemplates.RemoveRange(template.Steps);
        template.Steps.Clear();
    }

    public Task AddWorkflowStepsAsync(IEnumerable<WorkflowStepTemplate> steps, CancellationToken ct = default) =>
        db.WorkflowStepTemplates.AddRangeAsync(steps, ct);

    public void RemoveWorkflow(WorkflowTemplate template) =>
        db.WorkflowTemplates.Remove(template);

    public async Task DeactivateWorkflowsAsync(string scenarioCode, CancellationToken ct = default) =>
        await db.WorkflowTemplates
            .Where(x => x.ScenarioCode == scenarioCode && x.IsActive)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, false), ct);

    public Task<AgentTemplate?> FindAgentAsync(string scenarioCode, string agentCode, CancellationToken ct = default) =>
        db.AgentTemplates.FirstOrDefaultAsync(
            x => x.ScenarioCode == scenarioCode && x.AgentCode == agentCode, ct);

    public Task<AgentTemplate?> FindAgentByIdAsync(Guid id, CancellationToken ct = default) =>
        db.AgentTemplates.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<List<AgentTemplate>> ListAgentsAsync(string? scenarioCode, CancellationToken ct = default)
    {
        var query = db.AgentTemplates.AsQueryable();
        if (!string.IsNullOrWhiteSpace(scenarioCode))
            query = query.Where(x => x.ScenarioCode == scenarioCode);
        return query.OrderBy(x => x.ScenarioCode).ThenBy(x => x.AgentCode).ToListAsync(ct);
    }

    public async Task AddAgentAsync(AgentTemplate template, CancellationToken ct = default) =>
        await db.AgentTemplates.AddAsync(template, ct);

    public void RemoveAgent(AgentTemplate template) =>
        db.AgentTemplates.Remove(template);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
