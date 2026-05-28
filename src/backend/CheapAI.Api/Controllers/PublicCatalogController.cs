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
    public async Task<IActionResult> GetLatestTests([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? testType = null, CancellationToken cancellationToken = default)
    {
        var result = await publicCatalogService.GetLatestTestsAsync(page, pageSize, testType, cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext, result));
    }

    [HttpGet("tests/{id}")]
    public async Task<IActionResult> GetTestDetail(string id, CancellationToken cancellationToken = default)
    {
        var result = await publicCatalogService.GetTestDetailAsync(id, cancellationToken);
        if (result is null)
        {
            return NotFound(ApiResponseFactory.Failure<object?>(HttpContext, 40401, "Test record does not exist.", null));
        }

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

}
