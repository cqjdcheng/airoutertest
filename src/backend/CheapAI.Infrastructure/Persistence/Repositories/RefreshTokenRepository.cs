using CheapAI.Application.Admin.Repositories;
using CheapAI.Domain.Admin;
using CheapAI.Infrastructure.Persistence.Entities;
using SqlSugar;

namespace CheapAI.Infrastructure.Persistence.Repositories;

public sealed class RefreshTokenRepository(ISqlSugarClient db) : IRefreshTokenRepository
{
    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        var entity = await db.Queryable<RefreshTokenEntity>()
            .FirstAsync(x => x.TokenHash == tokenHash, cancellationToken);

        return entity is null
            ? null
            : new RefreshToken
            {
                Id = entity.Id,
                AdminUserId = entity.AdminUserId,
                TokenHash = entity.TokenHash,
                UserAgent = entity.UserAgent,
                IpAddress = entity.IpAddress,
                ExpiresAtUtc = entity.ExpiresAt,
                RevokedAtUtc = entity.RevokedAt,
                CreatedAtUtc = entity.CreatedAt
            };
    }

    public async Task<ulong> InsertAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        var entity = new RefreshTokenEntity
        {
            AdminUserId = refreshToken.AdminUserId,
            TokenHash = refreshToken.TokenHash,
            UserAgent = refreshToken.UserAgent,
            IpAddress = refreshToken.IpAddress,
            ExpiresAt = refreshToken.ExpiresAtUtc,
            RevokedAt = refreshToken.RevokedAtUtc,
            CreatedAt = refreshToken.CreatedAtUtc
        };

        return (ulong)await db.Insertable(entity).ExecuteReturnBigIdentityAsync();
    }

    public Task RevokeAsync(ulong refreshTokenId, DateTime revokedAtUtc, CancellationToken cancellationToken = default)
    {
        return db.Updateable<RefreshTokenEntity>()
            .SetColumns(x => new RefreshTokenEntity { RevokedAt = revokedAtUtc })
            .Where(x => x.Id == refreshTokenId)
            .ExecuteCommandAsync(cancellationToken);
    }
}
