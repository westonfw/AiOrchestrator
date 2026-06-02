using AiOrchestrator.Application;
using Microsoft.AspNetCore.Mvc;

namespace AiOrchestrator.Api.Controllers;

[ApiController]
[Route("api/scenarios")]
public sealed class ScenariosController : ControllerBase
{
    [HttpGet("{scenarioCode}/workflow")]
    public async Task<ActionResult<ApiResponse<object>>> GetWorkflowAsync(
        string scenarioCode,
        IWorkflowDefinitionLoader loader,
        CancellationToken ct)
    {
        var workflow = await loader.LoadAsync(scenarioCode, ct);
        return Ok(ApiResponse<object>.Ok(workflow));
    }
}
