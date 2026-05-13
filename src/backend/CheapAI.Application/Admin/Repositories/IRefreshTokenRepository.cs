using CheapAI.Domain.Admin;

namespace CheapAI.Application.Admin.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task<ulong> InsertAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

    Task RevokeAsync(ulong refreshTokenId, DateTime revokedAtUtc, CancellationToken cancellationToken = default);
}
