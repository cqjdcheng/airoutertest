using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using CheapAI.Application.Auth;
using CheapAI.Application.Models;
using CheapAI.Application.Operations;
using CheapAI.Application.Participation;
using CheapAI.Application.Public;
using CheapAI.Application.RelaySites;

namespace CheapAI.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<AdminAuthService>();
        services.AddScoped<RelaySiteAdminService>();
        services.AddScoped<ModelAdminService>();
        services.AddScoped<OperationsAdminService>();
        services.AddScoped<PublicParticipationService>();
        services.AddScoped<AdminParticipationService>();
        services.AddScoped<PublicOverviewService>();
        services.AddScoped<PublicRankingService>();
        services.AddScoped<PublicSiteDetailService>();
        services.AddScoped<PublicCatalogService>();
        return services;
    }
}
