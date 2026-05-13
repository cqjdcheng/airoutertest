using CheapAI.Api.Common;
using CheapAI.Application.Public;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CheapAI.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/public/rankings/models")]
public sealed class PublicRankingsController(PublicRankingService rankingService) : ControllerBase
{
    [HttpGet("{modelSlug}")]
    public async Task<IActionResult> GetByModel(string modelSlug, [FromQuery] ModelRankingQuery query, CancellationToken cancellationToken)
    {
        var result = await rankingService.GetAsync(modelSlug, query, cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext, result));
    }
}
