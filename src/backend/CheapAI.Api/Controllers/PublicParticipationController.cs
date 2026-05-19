using CheapAI.Api.Common;
using CheapAI.Application.Participation;
using Microsoft.AspNetCore.Mvc;

namespace CheapAI.Api.Controllers;

[ApiController]
[Route("api/v1/public")]
public sealed class PublicParticipationController(PublicParticipationService publicParticipationService) : ControllerBase
{
    [HttpGet("self-tests/challenge")]
    public IActionResult CreateSelfTestChallenge()
    {
        var result = publicParticipationService.CreateSelfTestChallenge();
        return Ok(ApiResponseFactory.Success(HttpContext, result));
    }

    [HttpPost("self-tests")]
    public async Task<IActionResult> CreateSelfTest([FromBody] CreateSelfTestRequest request, CancellationToken cancellationToken)
    {
        var result = await publicParticipationService.CreateSelfTestAsync(request, cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext, result));
    }

    [HttpGet("self-tests/{id}")]
    public async Task<IActionResult> GetSelfTest(string id, CancellationToken cancellationToken)
    {
        var result = await publicParticipationService.GetSelfTestAsync(id, cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext, result));
    }

    [HttpPost("submissions")]
    public async Task<IActionResult> CreateSubmission([FromBody] CreateSiteSubmissionRequest request, CancellationToken cancellationToken)
    {
        var id = await publicParticipationService.CreateSubmissionAsync(request, cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext, new { id }));
    }

    [HttpGet("model-capabilities")]
    public async Task<IActionResult> GetCapabilities(CancellationToken cancellationToken)
    {
        var result = await publicParticipationService.GetCapabilityRankingAsync(cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext, result));
    }

    [HttpGet("articles")]
    public async Task<IActionResult> GetArticles([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? tag = null, CancellationToken cancellationToken = default)
    {
        var result = await publicParticipationService.GetArticlesAsync(page, pageSize, tag, cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext, result));
    }

    [HttpGet("article-tags")]
    public async Task<IActionResult> GetArticleTags(CancellationToken cancellationToken)
    {
        var result = await publicParticipationService.GetArticleTagsAsync(cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext, result));
    }

    [HttpGet("articles/{slug}")]
    public async Task<IActionResult> GetArticle(string slug, CancellationToken cancellationToken)
    {
        var result = await publicParticipationService.GetArticleBySlugAsync(slug, cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext, result));
    }
}
