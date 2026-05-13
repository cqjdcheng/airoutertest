using CheapAI.Api.Common;
using CheapAI.Application.RelaySites;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CheapAI.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/admin/sites")]
public sealed class AdminSitesController(RelaySiteAdminService relaySiteAdminService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] RelaySiteListQuery query, CancellationToken cancellationToken)
    {
        var result = await relaySiteAdminService.GetPagedAsync(query, cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext, result));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetDetail(ulong id, CancellationToken cancellationToken)
    {
        var result = await relaySiteAdminService.GetByIdAsync(id, cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext, result));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRelaySiteRequest request, CancellationToken cancellationToken)
    {
        var id = await relaySiteAdminService.CreateAsync(request, cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext, new { id }));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(ulong id, [FromBody] UpdateRelaySiteRequest request, CancellationToken cancellationToken)
    {
        await relaySiteAdminService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext));
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(ulong id, [FromBody] UpdateRelaySiteStatusRequest request, CancellationToken cancellationToken)
    {
        await relaySiteAdminService.UpdateStatusAsync(id, request, cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext));
    }
}
