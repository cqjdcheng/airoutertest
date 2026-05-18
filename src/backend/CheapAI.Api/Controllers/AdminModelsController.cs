using CheapAI.Api.Common;
using CheapAI.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CheapAI.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/admin/models")]
public sealed class AdminModelsController(ModelAdminService modelAdminService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] ModelListQuery query, CancellationToken cancellationToken)
    {
        var result = await modelAdminService.GetPagedAsync(query, cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext, result));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetDetail(ulong id, CancellationToken cancellationToken)
    {
        var result = await modelAdminService.GetByIdAsync(id, cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext, result));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateModelRequest request, CancellationToken cancellationToken)
    {
        var id = await modelAdminService.CreateAsync(request, cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext, new { id }));
    }

    [HttpPost("import-preview")]
    public async Task<IActionResult> PreviewImport([FromBody] ModelImportPreviewRequest request, CancellationToken cancellationToken)
    {
        var result = await modelAdminService.PreviewImportAsync(request, cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext, result));
    }

    [HttpPost("openrouter-preview")]
    public async Task<IActionResult> PreviewOpenRouterImport(CancellationToken cancellationToken)
    {
        var result = await modelAdminService.PreviewOpenRouterImportAsync(cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext, result));
    }

    [HttpPost("import")]
    public async Task<IActionResult> Import([FromBody] ImportModelsRequest request, CancellationToken cancellationToken)
    {
        var result = await modelAdminService.ImportAsync(request, cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext, result));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(ulong id, [FromBody] UpdateModelRequest request, CancellationToken cancellationToken)
    {
        await modelAdminService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext));
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(ulong id, [FromBody] UpdateModelStatusRequest request, CancellationToken cancellationToken)
    {
        await modelAdminService.UpdateStatusAsync(id, request, cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext));
    }

    [HttpPatch("{id}/metadata")]
    public async Task<IActionResult> UpdateMetadata(ulong id, [FromBody] UpdateModelMetadataRequest request, CancellationToken cancellationToken)
    {
        await modelAdminService.UpdateMetadataAsync(id, request, cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext));
    }
}
