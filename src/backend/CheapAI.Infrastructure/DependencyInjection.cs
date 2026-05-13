using CheapAI.Application.Admin.Repositories;
using CheapAI.Application.Common.Abstractions;
using CheapAI.Application.Models;
using CheapAI.Application.Operations;
using CheapAI.Application.Participation;
using CheapAI.Application.Public;
using CheapAI.Application.RelaySites;
using CheapAI.Infrastructure.Caching;
using CheapAI.Infrastructure.Jobs;
using CheapAI.Infrastructure.Participation;
using CheapAI.Infrastructure.Persistence;
using CheapAI.Infrastructure.Persistence.Repositories;
using CheapAI.Infrastructure.Security;
using Hangfire;
using Hangfire.MySql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using SqlSugar;
using StackExchange.Redis;

namespace CheapAI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MySqlOptions>(configuration.GetSection(MySqlOptions.SectionName));
        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));
        services.Configure<HangfireOptions>(configuration.GetSection(HangfireOptions.SectionName));

        var mySqlConnectionString = configuration[$"{MySqlOptions.SectionName}:ConnectionString"]
            ?? throw new InvalidOperationException("Missing MySQL connection string configuration.");

        var redisConnectionString = configuration[$"{RedisOptions.SectionName}:ConnectionString"]
            ?? throw new InvalidOperationException("Missing Redis connection string configuration.");
        var hangfireOptions = configuration.GetSection(HangfireOptions.SectionName).Get<HangfireOptions>() ?? new HangfireOptions();

        var runtimeState = new InfrastructureRuntimeState();
        services.AddSingleton(runtimeState);

        services.AddSingleton<ISqlSugarClient>(_ => new SqlSugarScope(new ConnectionConfig
        {
            ConnectionString = mySqlConnectionString,
            DbType = DbType.MySql,
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.Attribute
        }));

        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));

        if (CanReachMySqlPort(mySqlConnectionString))
        {
            services.AddHangfire(config =>
            {
                config
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UseStorage(new MySqlStorage(mySqlConnectionString, new MySqlStorageOptions()));
            });

            runtimeState.HangfireEnabled = true;
            if (hangfireOptions.RunServer)
            {
                services.AddHangfireServer();
            }
        }
        else
        {
            runtimeState.HangfireEnabled = false;
            runtimeState.HangfireDisabledReason = "MySQL 监听不可达，已跳过 Hangfire 初始化。";
            Console.WriteLine("[CheapAI] Hangfire disabled because MySQL endpoint is not reachable.");
        }

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IRefreshTokenGenerator, RefreshTokenGenerator>();
        services.AddSingleton<IAccessTokenService, JwtAccessTokenService>();
        services.AddSingleton<ISelfTestRunner, OpenAiCompatibleSelfTestRunner>();
        services.AddSingleton<ISelfTestChallengeService, InMemorySelfTestChallengeService>();

        services.AddScoped<IAdminUserRepository, AdminUserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IRelaySiteRepository, RelaySiteRepository>();
        services.AddScoped<IModelRepository, ModelRepository>();
        services.AddScoped<IModelRankingSnapshotRepository, ModelRankingSnapshotRepository>();
        services.AddScoped<IPublicSiteQueryRepository, PublicSiteQueryRepository>();
        services.AddScoped<IPublicCatalogRepository, PublicCatalogRepository>();
        services.AddScoped<IOperationsRepository, OperationsRepository>();
        services.AddScoped<IParticipationRepository, ParticipationRepository>();

        return services;
    }

    private static bool CanReachMySqlPort(string connectionString)
    {
        try
        {
            var builder = new MySqlConnectionStringBuilder(connectionString);
            using var tcpClient = new System.Net.Sockets.TcpClient();
            var task = tcpClient.ConnectAsync(builder.Server, (int)builder.Port);
            var completed = task.Wait(TimeSpan.FromSeconds(2));
            return completed && tcpClient.Connected;
        }
        catch
        {
            return false;
        }
    }
}
