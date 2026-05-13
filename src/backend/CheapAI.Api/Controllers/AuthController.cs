using CheapAI.Api.Common;
using CheapAI.Application.Auth;
using CheapAI.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CheapAI.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(
    AdminAuthService authService,
    IOptions<JwtOptions> jwtOptions,
    IWebHostEnvironment environment) : ControllerBase
{
    private const string RefreshTokenCookieName = "cheapai_rt";

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] AdminLoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, Request.Headers.UserAgent.ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        SetRefreshTokenCookie(result.RefreshToken);
        return Ok(ApiResponseFactory.Success(HttpContext, new
        {
            result.AccessToken,
            result.ExpiresIn,
            result.Admin
        }));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        if (!Request.Cookies.TryGetValue(RefreshTokenCookieName, out var refreshToken) || string.IsNullOrWhiteSpace(refreshToken))
        {
            return Unauthorized(ApiResponseFactory.Failure<object?>(HttpContext, 40101, "refresh token missing", null));
        }

        var result = await authService.RefreshAsync(refreshToken, Request.Headers.UserAgent.ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        SetRefreshTokenCookie(result.RefreshToken);
        return Ok(ApiResponseFactory.Success(HttpContext, new
        {
            result.AccessToken,
            result.ExpiresIn,
            result.Admin
        }));
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (Request.Cookies.TryGetValue(RefreshTokenCookieName, out var refreshToken) && !string.IsNullOrWhiteSpace(refreshToken))
        {
            await authService.LogoutAsync(refreshToken, cancellationToken);
        }

        Response.Cookies.Delete(RefreshTokenCookieName);
        return Ok(ApiResponseFactory.Success(HttpContext));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var result = await authService.GetCurrentAsync(cancellationToken);
        return Ok(ApiResponseFactory.Success(HttpContext, result));
    }

    private void SetRefreshTokenCookie(string refreshToken)
    {
        Response.Cookies.Append(RefreshTokenCookieName, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Secure = !environment.IsDevelopment(),
            Expires = DateTimeOffset.UtcNow.AddDays(jwtOptions.Value.RefreshTokenDays)
        });
    }
}
