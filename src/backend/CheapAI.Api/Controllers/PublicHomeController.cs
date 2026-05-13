using CheapAI.Api.Common;
using CheapAI.Application.Public;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CheapAI.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/public/home")]
public sealed class PublicHomeController(PublicOverviewService overviewService) : ControllerBase
{
    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(CancellationToken cancellationToken)
    {
        var result = await overviewService.GetAsync(cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext, result));
    }
}
