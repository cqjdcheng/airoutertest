using CheapAI.Api.Common;
using CheapAI.Application.Participation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CheapAI.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/admin")]
public sealed class AdminParticipationController(AdminParticipationService adminParticipationService) : ControllerBase
{
    [HttpGet("submissions")]
    public async Task<IActionResult> GetSubmissions([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await adminParticipationService.GetSubmissionsAsync(page, pageSize, cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext, result));
    }

    [HttpPost("submissions/{id}/approve")]
    public async Task<IActionResult> ApproveSubmission(ulong id, [FromBody] ReviewSubmissionRequest request, CancellationToken cancellationToken)
    {
        await adminParticipationService.ApproveSubmissionAsync(id, request, cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext));
    }

    [HttpPost("submissions/{id}/reject")]
    public async Task<IActionResult> RejectSubmission(ulong id, [FromBody] ReviewSubmissionRequest request, CancellationToken cancellationToken)
    {
        await adminParticipationService.RejectSubmissionAsync(id, request, cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext));
    }

    [HttpGet("articles")]
    public async Task<IActionResult> GetArticles([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await adminParticipationService.GetArticlesAsync(page, pageSize, cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext, result));
    }

    [HttpGet("articles/{id}")]
    public async Task<IActionResult> GetArticle(ulong id, CancellationToken cancellationToken)
    {
        var result = await adminParticipationService.GetArticleByIdAsync(id, cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext, result));
    }

    [HttpPost("articles")]
    public async Task<IActionResult> CreateArticle([FromBody] CreateArticleRequest request, CancellationToken cancellationToken)
    {
        var id = await adminParticipationService.CreateArticleAsync(request, cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext, new { id }));
    }

    [HttpPut("articles/{id}")]
    public async Task<IActionResult> UpdateArticle(ulong id, [FromBody] UpdateArticleRequest request, CancellationToken cancellationToken)
    {
        await adminParticipationService.UpdateArticleAsync(id, request, cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext));
    }

    [HttpPatch("articles/{id}/publish")]
    public async Task<IActionResult> PublishArticle(ulong id, CancellationToken cancellationToken)
    {
        await adminParticipationService.PublishArticleAsync(id, cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext));
    }

    [HttpPatch("articles/{id}/archive")]
    public async Task<IActionResult> ArchiveArticle(ulong id, CancellationToken cancellationToken)
    {
        await adminParticipationService.ArchiveArticleAsync(id, cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext));
    }

    [HttpDelete("articles/{id}")]
    public async Task<IActionResult> DeleteArticle(ulong id, CancellationToken cancellationToken)
    {
        await adminParticipationService.DeleteArticleAsync(id, cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext));
    }

    [HttpGet("article-tags")]
    public async Task<IActionResult> GetArticleTags(CancellationToken cancellationToken)
    {
        var result = await adminParticipationService.GetArticleTagsAsync(cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext, result));
    }

    [HttpPost("article-tags")]
    public async Task<IActionResult> CreateArticleTag([FromBody] UpsertArticleTagRequest request, CancellationToken cancellationToken)
    {
        var id = await adminParticipationService.CreateArticleTagAsync(request, cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext, new { id }));
    }

    [HttpPut("article-tags/{id}")]
    public async Task<IActionResult> UpdateArticleTag(ulong id, [FromBody] UpsertArticleTagRequest request, CancellationToken cancellationToken)
    {
        await adminParticipationService.UpdateArticleTagAsync(id, request, cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext));
    }

    [HttpDelete("article-tags/{id}")]
    public async Task<IActionResult> DeleteArticleTag(ulong id, CancellationToken cancellationToken)
    {
        await adminParticipationService.DeleteArticleTagAsync(id, cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext));
    }
}
