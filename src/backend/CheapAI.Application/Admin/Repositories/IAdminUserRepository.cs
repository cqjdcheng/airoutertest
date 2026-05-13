using CheapAI.Domain.Admin;

namespace CheapAI.Application.Admin.Repositories;

public interface IAdminUserRepository
{
    Task<AdminUser?> GetByIdAsync(ulong id, CancellationToken cancellationToken = default);

    Task<AdminUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    Task<long> CountAsync(CancellationToken cancellationToken = default);

    Task<ulong> InsertAsync(AdminUser adminUser, CancellationToken cancellationToken = default);

    Task UpdateLastLoginAtAsync(ulong id, DateTime lastLoginAtUtc, CancellationToken cancellationToken = default);
}
