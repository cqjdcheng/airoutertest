using CheapAI.Application.Admin.Repositories;
using CheapAI.Domain.Admin;
using CheapAI.Infrastructure.Persistence.Entities;
using SqlSugar;

namespace CheapAI.Infrastructure.Persistence.Repositories;

public sealed class AdminUserRepository(ISqlSugarClient db) : IAdminUserRepository
{
    public async Task<long> CountAsync(CancellationToken cancellationToken = default)
    {
        return await db.Queryable<AdminUserEntity>().CountAsync(cancellationToken);
    }

    public async Task<AdminUser?> GetByIdAsync(ulong id, CancellationToken cancellationToken = default)
    {
        var entity = await db.Queryable<AdminUserEntity>()
            .FirstAsync(x => x.Id == id, cancellationToken);

        return entity is null ? null : Map(entity);
    }

    public async Task<AdminUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var entity = await db.Queryable<AdminUserEntity>()
            .FirstAsync(x => x.Username == username, cancellationToken);

        return entity is null ? null : Map(entity);
    }

    public async Task<ulong> InsertAsync(AdminUser adminUser, CancellationToken cancellationToken = default)
    {
        var entity = new AdminUserEntity
        {
            Username = adminUser.Username,
            PasswordHash = adminUser.PasswordHash,
            DisplayName = adminUser.DisplayName,
            Email = adminUser.Email,
            Phone = adminUser.Phone,
            Status = adminUser.Status == AdminUserStatus.Active ? "active" : "suspended",
            LastLoginAt = adminUser.LastLoginAtUtc,
            CreatedAt = adminUser.CreatedAtUtc,
            UpdatedAt = adminUser.UpdatedAtUtc
        };

        return (ulong)await db.Insertable(entity).ExecuteReturnBigIdentityAsync();
    }

    public Task UpdateLastLoginAtAsync(ulong id, DateTime lastLoginAtUtc, CancellationToken cancellationToken = default)
    {
        return db.Updateable<AdminUserEntity>()
            .SetColumns(x => new AdminUserEntity
            {
                LastLoginAt = lastLoginAtUtc,
                UpdatedAt = DateTime.UtcNow
            })
            .Where(x => x.Id == id)
            .ExecuteCommandAsync(cancellationToken);
    }

    private static AdminUser Map(AdminUserEntity entity)
    {
        return new AdminUser
        {
            Id = entity.Id,
            Username = entity.Username,
            PasswordHash = entity.PasswordHash,
            DisplayName = entity.DisplayName,
            Email = entity.Email,
            Phone = entity.Phone,
            Status = entity.Status == "active" ? AdminUserStatus.Active : AdminUserStatus.Suspended,
            LastLoginAtUtc = entity.LastLoginAt,
            CreatedAtUtc = entity.CreatedAt,
            UpdatedAtUtc = entity.UpdatedAt
        };
    }
}
