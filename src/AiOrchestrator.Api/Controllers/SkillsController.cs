using AiOrchestrator.Application;
using Microsoft.AspNetCore.Mvc;

namespace AiOrchestrator.Api.Controllers;

[ApiController]
[Route("api/skills")]
public sealed class SkillsController : ControllerBase
{
    [HttpGet]
    public ActionResult<ApiResponse<object>> List(ISkillRegistry registry)
    {
        var skills = registry.List().Select(s => new
        {
            s.Code, s.Name, s.Description, s.IsSensitive, s.RequireReview
        });
        return Ok(ApiResponse<object>.Ok(new { Items = skills }));
    }
}
