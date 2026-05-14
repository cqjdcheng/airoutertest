using CheapAI.Api.Common;
using CheapAI.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CheapAI.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/admin/model-providers")]
public sealed class AdminModelProvidersController(ModelProviderAdminService modelProviderAdminService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] ModelProviderListQuery query, CancellationToken cancellationToken)
    {
        var result = await modelProviderAdminService.GetPagedAsync(query, cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext, result));
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
    {
        var result = await modelProviderAdminService.GetAllActiveAsync(cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext, result));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetDetail(ulong id, CancellationToken cancellationToken)
    {
        var result = await modelProviderAdminService.GetByIdAsync(id, cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext, result));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateModelProviderRequest request, CancellationToken cancellationToken)
    {
        var id = await modelProviderAdminService.CreateAsync(request, cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext, new { id }));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(ulong id, [FromBody] UpdateModelProviderRequest request, CancellationToken cancellationToken)
    {
        await modelProviderAdminService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext));
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(ulong id, [FromBody] UpdateModelProviderStatusRequest request, CancellationToken cancellationToken)
    {
        await modelProviderAdminService.UpdateStatusAsync(id, request, cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext));
    }
}
