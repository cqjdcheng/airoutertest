using CheapAI.Api.Common;
using CheapAI.Application.SiteSettings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CheapAI.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/public/site-settings")]
public sealed class PublicSiteSettingsController(SiteSettingsService siteSettingsService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await siteSettingsService.GetAsync(cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext, result));
    }
}
