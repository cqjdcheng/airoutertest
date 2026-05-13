using CheapAI.Api.Common;
using CheapAI.Application.Public;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CheapAI.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/public")]
public sealed class PublicCatalogController(PublicCatalogService publicCatalogService) : ControllerBase
{
    [HttpGet("tests/latest")]
    public async Task<IActionResult> GetLatestTests([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await publicCatalogService.GetLatestTestsAsync(page, pageSize, cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext, result));
    }

    [HttpGet("sites")]
    public async Task<IActionResult> GetSites([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await publicCatalogService.GetRelaySitesAsync(page, pageSize, cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext, result));
    }

    [HttpGet("models")]
    public async Task<IActionResult> GetModels([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await publicCatalogService.GetModelsAsync(page, pageSize, cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext, result));
    }

    [HttpGet("rankings/cheapest")]
    public async Task<IActionResult> GetCheapestRankings([FromQuery] string? modelSlugs = null, [FromQuery] int limit = 5, CancellationToken cancellationToken = default)
    {
        var slugs = string.IsNullOrWhiteSpace(modelSlugs)
            ? new[] { "gpt-4-1-mini", "claude-sonnet-4", "gemini-2-5-pro", "deepseek-chat", "qwen-max" }
            : modelSlugs.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var result = await publicCatalogService.GetCheapestRankingsAsync(slugs, limit, cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext, result));
    }
}
