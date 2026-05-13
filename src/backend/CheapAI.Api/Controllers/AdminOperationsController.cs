using CheapAI.Api.Common;
using CheapAI.Application.Operations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CheapAI.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/admin")]
public sealed class AdminOperationsController(OperationsAdminService operationsAdminService) : ControllerBase
{
    [HttpGet("offers")]
    public async Task<IActionResult> GetOffers([FromQuery] OperationListQuery query, CancellationToken cancellationToken)
    {
        var result = await operationsAdminService.GetOffersAsync(query, cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext, result));
    }

    [HttpGet("test-records")]
    public async Task<IActionResult> GetTestRecords([FromQuery] OperationListQuery query, CancellationToken cancellationToken)
    {
        var result = await operationsAdminService.GetTestRecordsAsync(query, cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext, result));
    }

    [HttpGet("risks")]
    public async Task<IActionResult> GetRisks([FromQuery] OperationListQuery query, CancellationToken cancellationToken)
    {
        var result = await operationsAdminService.GetRisksAsync(query, cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext, result));
    }

    [HttpGet("risks/{riskId}")]
    public async Task<IActionResult> GetRisk(ulong riskId, CancellationToken cancellationToken)
    {
        var result = await operationsAdminService.GetRiskAsync(riskId, cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext, result));
    }

    [HttpPost("risks/{riskId}/review")]
    public async Task<IActionResult> ReviewRisk(ulong riskId, [FromBody] ReviewRiskEvidenceRequest request, CancellationToken cancellationToken)
    {
        await operationsAdminService.ReviewRiskAsync(riskId, request, cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext));
    }

    [HttpGet("job-logs")]
    public async Task<IActionResult> GetJobLogs([FromQuery] OperationListQuery query, CancellationToken cancellationToken)
    {
        var result = await operationsAdminService.GetJobLogsAsync(query, cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext, result));
    }

    [HttpPost("offers/{offerId}/verify")]
    public async Task<IActionResult> VerifyOffer(ulong offerId, CancellationToken cancellationToken)
    {
        await operationsAdminService.VerifyOfferAsync(offerId, cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext));
    }

    [HttpPost("crawl-jobs/manual-run")]
    public async Task<IActionResult> RunManualCrawl(CancellationToken cancellationToken)
    {
        var result = await operationsAdminService.RunManualCrawlAsync(cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext, result));
    }

    [HttpPost("test-jobs/manual-run")]
    public async Task<IActionResult> RunManualTest(CancellationToken cancellationToken)
    {
        var result = await operationsAdminService.RunManualTestAsync(cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext, result));
    }

    [HttpPost("risks/recalculate")]
    public async Task<IActionResult> RecalculateRisks(CancellationToken cancellationToken)
    {
        var result = await operationsAdminService.RecalculateRisksAsync(cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext, result));
    }

    [HttpPost("rankings/rebuild")]
    public async Task<IActionResult> RebuildRankings(CancellationToken cancellationToken)
    {
        var result = await operationsAdminService.RebuildRankingsAsync(cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext, result));
    }
}
