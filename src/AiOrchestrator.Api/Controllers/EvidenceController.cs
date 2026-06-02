using AiOrchestrator.Application;
using Microsoft.AspNetCore.Mvc;

namespace AiOrchestrator.Api.Controllers;

[ApiController]
public sealed class EvidenceController : ControllerBase
{
    [HttpGet("api/tasks/{taskId:guid}/evidence")]
    public async Task<ActionResult<ApiResponse<object>>> ListByTaskAsync(
        Guid taskId,
        IOrchestrationStore store,
        CancellationToken ct)
    {
        var evidence = await store.ListEvidenceAsync(taskId, ct);
        return Ok(ApiResponse<object>.Ok(new { Items = evidence }));
    }

    [HttpPost("api/evidence/{evidenceId:guid}/verify")]
    public async Task<ActionResult<ApiResponse<object>>> VerifyAsync(
        Guid evidenceId,
        IOrchestrationStore store,
        CancellationToken ct)
    {
        var evidence = await store.FindEvidenceAsync(evidenceId, ct);
        if (evidence is null)
        {
            return NotFound(ApiResponse<object>.Fail("not_found", "Evidence not found."));
        }

        evidence.Verified = true;
        await store.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { evidence.Id, evidence.Verified }));
    }
}
