using CheapAI.Application.Admin.Repositories;
using CheapAI.Application.Common.Abstractions;
using CheapAI.Api.Common;
using CheapAI.Domain.Admin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CheapAI.Api.HostedServices;

public sealed class BootstrapAdminHostedService(
    IServiceScopeFactory serviceScopeFactory,
    IPasswordHasher passwordHasher,
    IOptions<BootstrapAdminOptions> options,
    ILogger<BootstrapAdminHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var config = options.Value;
            if (!config.Enabled)
            {
                return;
            }

            using var scope = serviceScopeFactory.CreateScope();
            var adminUserRepository = scope.ServiceProvider.GetRequiredService<IAdminUserRepository>();

            if (await adminUserRepository.CountAsync(cancellationToken) > 0)
            {
                return;
            }

            var adminUser = new AdminUser
            {
                Username = config.Username,
                PasswordHash = passwordHasher.Hash(config.Password),
                DisplayName = config.DisplayName,
                Status = AdminUserStatus.Active,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            var adminUserId = await adminUserRepository.InsertAsync(adminUser, cancellationToken);
            logger.LogInformation("Bootstrapped default admin user with id {AdminUserId}.", adminUserId);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Bootstrap admin initialization skipped because database is not currently reachable.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
