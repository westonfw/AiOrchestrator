using AiOrchestrator.Application;
using Microsoft.AspNetCore.Mvc;

namespace AiOrchestrator.Api.Controllers;

[ApiController]
public sealed class ArtifactsController : ControllerBase
{
    [HttpGet("api/tasks/{taskId:guid}/artifacts")]
    public async Task<ActionResult<ApiResponse<object>>> ListByTaskAsync(
        Guid taskId,
        IOrchestrationStore store,
        CancellationToken ct)
    {
        var artifacts = await store.ListArtifactsAsync(taskId, ct);
        return Ok(ApiResponse<object>.Ok(new { Items = artifacts }));
    }

    [HttpGet("api/artifacts/{artifactId:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> GetAsync(
        Guid artifactId,
        IOrchestrationStore store,
        CancellationToken ct)
    {
        var artifact = await store.FindArtifactAsync(artifactId, ct);
        return artifact is null
            ? NotFound(ApiResponse<object>.Fail("not_found", "Artifact not found."))
            : Ok(ApiResponse<object>.Ok(artifact));
    }
}
