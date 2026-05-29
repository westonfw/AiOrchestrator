using AiOrchestrator.Application;
using AiOrchestrator.Domain;
using Microsoft.EntityFrameworkCore;

namespace AiOrchestrator.Infrastructure;

public sealed class EfOrchestrationStore(AiOrchestratorDbContext db) : IOrchestrationStore
{
    public Task<AiTask?> FindTaskAsync(Guid taskId, CancellationToken ct = default) =>
        db.Tasks.FirstOrDefaultAsync(x => x.Id == taskId, ct);

    public Task<List<AiTask>> ListTasksAsync(string? scenarioCode, string? status, int pageIndex, int pageSize, CancellationToken ct = default)
    {
        var query = db.Tasks.AsQueryable();
        if (!string.IsNullOrWhiteSpace(scenarioCode))
        {
            query = query.Where(x => x.ScenarioCode == scenarioCode);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<Domain.TaskStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(x => x.Status == parsedStatus);
        }

        return query.OrderByDescending(x => x.CreatedAt)
            .Skip(Math.Max(0, pageIndex - 1) * Math.Clamp(pageSize, 1, 100))
            .Take(Math.Clamp(pageSize, 1, 100))
            .ToListAsync(ct);
    }

    public async Task AddTaskAsync(AiTask task, CancellationToken ct = default) => await db.Tasks.AddAsync(task, ct);

    public Task<WorkflowRun?> FindWorkflowRunAsync(Guid workflowRunId, CancellationToken ct = default) =>
        db.WorkflowRuns.FirstOrDefaultAsync(x => x.Id == workflowRunId, ct);

    public Task<WorkflowRun?> FindLatestWorkflowRunAsync(Guid taskId, CancellationToken ct = default) =>
        db.WorkflowRuns.Where(x => x.TaskId == taskId).OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(ct);

    public async Task AddWorkflowRunAsync(WorkflowRun workflowRun, CancellationToken ct = default) => await db.WorkflowRuns.AddAsync(workflowRun, ct);

    public Task<WorkflowStepRun?> FindStepRunAsync(Guid stepRunId, CancellationToken ct = default) =>
        db.WorkflowStepRuns.FirstOrDefaultAsync(x => x.Id == stepRunId, ct);

    public Task<List<WorkflowStepRun>> ListStepRunsAsync(Guid workflowRunId, CancellationToken ct = default) =>
        db.WorkflowStepRuns.Where(x => x.WorkflowRunId == workflowRunId).OrderBy(x => x.CreatedAt).ToListAsync(ct);

    public async Task AddStepRunAsync(WorkflowStepRun stepRun, CancellationToken ct = default) => await db.WorkflowStepRuns.AddAsync(stepRun, ct);

    public async Task AddSkillRunAsync(SkillRun skillRun, CancellationToken ct = default) => await db.SkillRuns.AddAsync(skillRun, ct);

    public async Task AddAgentRunAsync(AgentRun agentRun, CancellationToken ct = default) => await db.AgentRuns.AddAsync(agentRun, ct);

    public async Task AddEvidenceAsync(EvidenceItem evidenceItem, CancellationToken ct = default) => await db.EvidenceItems.AddAsync(evidenceItem, ct);

    public async Task AddArtifactAsync(Artifact artifact, CancellationToken ct = default) => await db.Artifacts.AddAsync(artifact, ct);

    public async Task AddReviewAsync(ReviewItem reviewItem, CancellationToken ct = default) => await db.ReviewItems.AddAsync(reviewItem, ct);

    public Task<ReviewItem?> FindReviewAsync(Guid reviewId, CancellationToken ct = default) =>
        db.ReviewItems.FirstOrDefaultAsync(x => x.Id == reviewId, ct);

    public Task<List<ReviewItem>> ListReviewsAsync(Guid? taskId, ReviewStatus? status, CancellationToken ct = default)
    {
        var query = db.ReviewItems.AsQueryable();
        if (taskId.HasValue)
        {
            query = query.Where(x => x.TaskId == taskId);
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        return query.OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
    }

    public Task<List<Artifact>> ListArtifactsAsync(Guid taskId, CancellationToken ct = default) =>
        db.Artifacts.Where(x => x.TaskId == taskId).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);

    public Task<Artifact?> FindArtifactAsync(Guid artifactId, CancellationToken ct = default) =>
        db.Artifacts.FirstOrDefaultAsync(x => x.Id == artifactId, ct);

    public Task<List<EvidenceItem>> ListEvidenceAsync(Guid taskId, CancellationToken ct = default) =>
        db.EvidenceItems.Where(x => x.TaskId == taskId).OrderBy(x => x.CreatedAt).ToListAsync(ct);

    public Task<EvidenceItem?> FindEvidenceAsync(Guid evidenceId, CancellationToken ct = default) =>
        db.EvidenceItems.FirstOrDefaultAsync(x => x.Id == evidenceId, ct);

    public Task<List<TraceEvent>> ListTraceAsync(Guid taskId, CancellationToken ct = default) =>
        db.TraceEvents.Where(x => x.TaskId == taskId).OrderBy(x => x.CreatedAt).ToListAsync(ct);

    public async Task AddTraceAsync(TraceEvent traceEvent, CancellationToken ct = default) => await db.TraceEvents.AddAsync(traceEvent, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
