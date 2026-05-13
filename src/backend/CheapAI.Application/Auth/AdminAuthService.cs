using CheapAI.Application.Admin.Repositories;
using CheapAI.Application.Common.Abstractions;
using CheapAI.Application.Common.Exceptions;
using CheapAI.Application.Common.Security;
using CheapAI.Application.Security;
using CheapAI.Domain.Admin;
using Microsoft.Extensions.Options;

namespace CheapAI.Application.Auth;

public sealed class AdminAuthService(
    IAdminUserRepository adminUserRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IPasswordHasher passwordHasher,
    IAccessTokenService accessTokenService,
    IRefreshTokenGenerator refreshTokenGenerator,
    IClock clock,
    ICurrentAdminAccessor currentAdminAccessor,
    IOptions<JwtOptions> jwtOptions)
{
    public async Task<AdminAuthResponse> LoginAsync(AdminLoginRequest request, string? userAgent, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var adminUser = await adminUserRepository.GetByUsernameAsync(request.Username, cancellationToken);
        if (adminUser is null || adminUser.Status != AdminUserStatus.Active || !passwordHasher.Verify(request.Password, adminUser.PasswordHash))
        {
            throw new AppUnauthorizedException("管理员账号或密码错误");
        }

        await adminUserRepository.UpdateLastLoginAtAsync(adminUser.Id, clock.UtcNow, cancellationToken);
        return await IssueTokensAsync(adminUser, userAgent, ipAddress, cancellationToken);
    }

    public async Task<AdminAuthResponse> RefreshAsync(string refreshToken, string? userAgent, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var tokenHash = TokenHashing.Sha256(refreshToken);
        var existing = await refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
        if (existing is null || existing.RevokedAtUtc.HasValue || existing.ExpiresAtUtc <= clock.UtcNow)
        {
            throw new AppUnauthorizedException("刷新令牌无效");
        }

        var adminUser = await adminUserRepository.GetByIdAsync(existing.AdminUserId, cancellationToken);
        if (adminUser is null || adminUser.Status != AdminUserStatus.Active)
        {
            throw new AppUnauthorizedException("管理员不可用");
        }

        await refreshTokenRepository.RevokeAsync(existing.Id, clock.UtcNow, cancellationToken);
        return await IssueTokensAsync(adminUser, userAgent, ipAddress, cancellationToken);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var tokenHash = TokenHashing.Sha256(refreshToken);
        var existing = await refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
        if (existing is null || existing.RevokedAtUtc.HasValue)
        {
            return;
        }

        await refreshTokenRepository.RevokeAsync(existing.Id, clock.UtcNow, cancellationToken);
    }

    public async Task<AdminMeResponse> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        if (!currentAdminAccessor.AdminUserId.HasValue)
        {
            throw new AppUnauthorizedException("未登录");
        }

        var adminUser = await adminUserRepository.GetByIdAsync(currentAdminAccessor.AdminUserId.Value, cancellationToken);
        if (adminUser is null)
        {
            throw new AppUnauthorizedException("管理员不存在");
        }

        return new AdminMeResponse
        {
            Id = adminUser.Id,
            Username = adminUser.Username,
            DisplayName = adminUser.DisplayName
        };
    }

    private async Task<AdminAuthResponse> IssueTokensAsync(AdminUser adminUser, string? userAgent, string? ipAddress, CancellationToken cancellationToken)
    {
        var jwt = jwtOptions.Value;
        var rawRefreshToken = refreshTokenGenerator.Generate();
        var refreshToken = new RefreshToken
        {
            AdminUserId = adminUser.Id,
            TokenHash = TokenHashing.Sha256(rawRefreshToken),
            UserAgent = userAgent,
            IpAddress = ipAddress,
            ExpiresAtUtc = clock.UtcNow.AddDays(jwt.RefreshTokenDays),
            CreatedAtUtc = clock.UtcNow
        };

        await refreshTokenRepository.InsertAsync(refreshToken, cancellationToken);

        return new AdminAuthResponse
        {
            AccessToken = accessTokenService.CreateToken(adminUser),
            ExpiresIn = jwt.AccessTokenMinutes * 60,
            RefreshToken = rawRefreshToken,
            Admin = new AdminMeResponse
            {
                Id = adminUser.Id,
                Username = adminUser.Username,
                DisplayName = adminUser.DisplayName
            }
        };
    }
}
