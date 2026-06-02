using AiOrchestrator.Application;
using Microsoft.AspNetCore.Mvc;

namespace AiOrchestrator.Api.Controllers;

[ApiController]
[Route("api/public-data")]
public sealed class PublicDataController : ControllerBase
{
    [HttpGet("companies/search")]
    public async Task<ActionResult<ApiResponse<object>>> SearchCompaniesAsync(
        [FromQuery] string keyword,
        [FromQuery] string? market,
        IPublicMarketDataProvider publicDataProvider,
        CancellationToken ct)
    {
        var results = await publicDataProvider.SearchCompaniesAsync(keyword, market, ct);
        return Ok(ApiResponse<object>.Ok(new { Items = results }));
    }

    [HttpGet("companies/{symbol}")]
    public async Task<ActionResult<ApiResponse<object>>> GetCompanyProfileAsync(
        string symbol,
        [FromQuery] string? market,
        IPublicMarketDataProvider publicDataProvider,
        CancellationToken ct)
    {
        var profile = await publicDataProvider.GetCompanyProfileAsync(symbol, market, ct);
        return profile is null
            ? NotFound(ApiResponse<object>.Fail("not_found", "Company profile not found."))
            : Ok(ApiResponse<object>.Ok(profile));
    }

    [HttpGet("quotes/{symbol}")]
    public async Task<ActionResult<ApiResponse<object>>> GetLatestQuoteAsync(
        string symbol,
        [FromQuery] string? market,
        IPublicMarketDataProvider publicDataProvider,
        CancellationToken ct)
    {
        var quote = await publicDataProvider.GetLatestQuoteAsync(symbol, market, ct);
        return quote is null
            ? NotFound(ApiResponse<object>.Fail("not_found", "Quote not found from configured public data source."))
            : Ok(ApiResponse<object>.Ok(quote));
    }
}
