using CheapAI.Api.Common;
using CheapAI.Application.SiteSettings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CheapAI.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/admin/site-settings")]
public sealed class AdminSiteSettingsController(SiteSettingsService siteSettingsService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await siteSettingsService.GetAsync(cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext, result));
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateSiteSettingsRequest request, CancellationToken cancellationToken)
    {
        await siteSettingsService.UpdateAsync(request, cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext));
    }
}
