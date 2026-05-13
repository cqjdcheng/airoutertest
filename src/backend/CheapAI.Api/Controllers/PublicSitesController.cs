using CheapAI.Api.Common;
using CheapAI.Application.Public;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CheapAI.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/public/sites")]
public sealed class PublicSitesController(PublicSiteDetailService publicSiteDetailService) : ControllerBase
{
    [HttpGet("{siteSlug}")]
    public async Task<IActionResult> GetDetail(string siteSlug, [FromQuery] string? window, [FromQuery] string? model, CancellationToken cancellationToken)
    {
        var result = await publicSiteDetailService.GetAsync(siteSlug, window, model, cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext, result));
    }
}
