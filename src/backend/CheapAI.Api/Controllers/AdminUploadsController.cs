using CheapAI.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CheapAI.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/admin/uploads")]
public sealed class AdminUploadsController(IHostEnvironment environment) : ControllerBase
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png",
        ".jpg",
        ".jpeg",
        ".webp",
        ".gif",
        ".svg",
        ".ico"
    };

    [HttpPost]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> Upload([FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length <= 0)
        {
            return BadRequest(ApiResponseFactory.Failure<object?>(HttpContext, 40001, "Empty file.", null));
        }

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension))
        {
            return BadRequest(ApiResponseFactory.Failure<object?>(HttpContext, 40002, "Unsupported file type.", null));
        }

        var relativeDirectory = DateTime.UtcNow.ToString("yyyy/MM");
        var uploadRoot = Path.Combine(environment.ContentRootPath, "wwwroot", "uploads");
        var targetDirectory = Path.Combine(uploadRoot, relativeDirectory);
        Directory.CreateDirectory(targetDirectory);

        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var targetPath = Path.Combine(targetDirectory, fileName);
        await using (var stream = System.IO.File.Create(targetPath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var url = $"/api/v1/public/uploads/{relativeDirectory}/{fileName}".Replace("\\", "/");
        return Ok(ApiResponseFactory.Success(HttpContext, new { url }));
    }
}
