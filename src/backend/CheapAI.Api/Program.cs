using System.Text;
using CheapAI.Application;
using CheapAI.Application.Common.Abstractions;
using CheapAI.Application.Security;
using CheapAI.Infrastructure;
using CheapAI.Api.Common;
using CheapAI.Api.HostedServices;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<BootstrapAdminOptions>(builder.Configuration.GetSection(BootstrapAdminOptions.SectionName));
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentAdminAccessor, CurrentAdminAccessor>();
builder.Services.AddHostedService<DatabaseInitializationHostedService>();
builder.Services.AddHostedService<BootstrapAdminHostedService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevelopmentCors", corsPolicyBuilder =>
    {
        corsPolicyBuilder
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .SetIsOriginAllowed(_ => true);
    });
});
builder.Services.AddScoped<FluentValidationActionFilter>();
builder.Services.AddControllers(options =>
{
    options.Filters.Add<FluentValidationActionFilter>();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
var infrastructureRuntimeState = app.Services.GetRequiredService<InfrastructureRuntimeState>();

app.UseSerilogRequestLogging();
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var response = error switch
        {
            CheapAI.Application.Common.Exceptions.AppNotFoundException => new { code = 40401, message = error.Message },
            CheapAI.Application.Common.Exceptions.AppConflictException => new { code = 40901, message = error.Message },
            CheapAI.Application.Common.Exceptions.AppUnauthorizedException => new { code = 40101, message = error.Message },
            _ => new { code = 50001, message = "Internal server error" }
        };

        context.Response.StatusCode = response.code switch
        {
            40401 => StatusCodes.Status404NotFound,
            40901 => StatusCodes.Status409Conflict,
            40101 => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status500InternalServerError
        };
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsJsonAsync(new
        {
            code = response.code,
            message = response.message,
            requestId = context.TraceIdentifier,
            timestamp = DateTime.UtcNow
        });
    });
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseCors("DevelopmentCors");
app.UseAuthorization();

app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger"));
if (app.Environment.IsDevelopment() && infrastructureRuntimeState.HangfireEnabled)
{
    app.MapHangfireDashboard("/hangfire");
}

app.Run();

public partial class Program;
