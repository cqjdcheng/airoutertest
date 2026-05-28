using CheapAI.Api.Common;
using CheapAI.Application.Public;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CheapAI.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/public/sites")]
public sealed class PublicSitesController(
    PublicSiteDetailService publicSiteDetailService,
    PublicOutboundClickService publicOutboundClickService) : ControllerBase
{
    [HttpGet("{siteSlug}")]
    public async Task<IActionResult> GetDetail(string siteSlug, [FromQuery] string? window, [FromQuery] string? model, CancellationToken cancellationToken)
    {
        var result = await publicSiteDetailService.GetAsync(siteSlug, window, model, cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext, result));
    }

    [HttpPost("{siteSlug}/outbound-clicks")]
    public async Task<IActionResult> RecordOutboundClick(
        string siteSlug,
        [FromBody] PublicOutboundClickRequest request,
        CancellationToken cancellationToken)
    {
        var targetType = PublicOutboundClickService.NormalizeTargetType(request.TargetType);
        if (targetType is null)
        {
            return BadRequest(ApiResponseFactory.Failure<object?>(
                HttpContext,
                40001,
                "Unsupported outbound target type.",
                null));
        }

        await publicOutboundClickService.RecordAsync(siteSlug, targetType, new PublicOutboundClickContext
        {
            SourcePath = request.SourcePath,
            Referrer = Request.Headers.Referer.ToString(),
            IpAddress = ResolveClientIp(HttpContext),
            UserAgent = Request.Headers.UserAgent.ToString()
        }, cancellationToken);

        return Ok(ApiResponseFactory.Success(HttpContext));
    }

    private static string? ResolveClientIp(HttpContext httpContext)
    {
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            return forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault();
        }

        return httpContext.Connection.RemoteIpAddress?.ToString();
    }
}
