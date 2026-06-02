using AiOrchestrator.Application;
using Microsoft.AspNetCore.Mvc;

namespace AiOrchestrator.Api.Controllers;

[ApiController]
public sealed class TraceController : ControllerBase
{
    [HttpGet("api/tasks/{taskId:guid}/trace")]
    public async Task<ActionResult<ApiResponse<object>>> ListByTaskAsync(
        Guid taskId,
        IOrchestrationStore store,
        CancellationToken ct)
    {
        var trace = await store.ListTraceAsync(taskId, ct);
        return Ok(ApiResponse<object>.Ok(new { Items = trace }));
    }
}
