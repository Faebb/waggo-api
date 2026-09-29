using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Serilog;
using Waggo.Api.Infrastructure.Authentication;
using Waggo.Api.Infrastructure.Authorization;
using Waggo.Api.Infrastructure.Errors;
using Waggo.Api.Infrastructure.Helpers;
using Waggo.Api.Infrastructure.Responses;
using Waggo.Api.Infrastructure.Settings;
using Waggo.Application.Common.Constants;

namespace Waggo.Api.Infrastructure.Extensions;

/// <summary>Registers every API service in the DI container. Each concern has its own method.</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWaggoApi(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        return services
            .AddWaggoLogging(configuration)
            .AddWaggoJson()
            .AddWaggoCors(configuration)
            .AddWaggoAuthentication(configuration, environment)
            .AddWaggoAuthorization()
            .AddWaggoRateLimiting(configuration)
            .AddWaggoRequestValidation()
            .AddOpenApi();
    }

    /// <summary>Serilog is the only provider; code logs through ILogger&lt;T&gt;.</summary>
    private static IServiceCollection AddWaggoLogging(this IServiceCollection services, IConfiguration configuration) =>
        services.AddSerilog((provider, logger) => logger
            .ReadFrom.Configuration(configuration)
            .ReadFrom.Services(provider)
            .Enrich.FromLogContext());

    private static IServiceCollection AddWaggoJson(this IServiceCollection services) =>
        services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

    private static IServiceCollection AddWaggoCors(this IServiceCollection services, IConfiguration configuration)
    {
        string[] origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        return services.AddCors(options => options.AddDefaultPolicy(policy =>
            policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));
    }

    /// <summary>
    /// OAuth 2.0: the API is a resource server that validates JWT access tokens of any provider.
    /// Development can use a fake user instead (never in Production).
    /// </summary>
    private static IServiceCollection AddWaggoAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        AuthenticationSettings settings =
            configuration.GetSection(AuthenticationSettings.SectionName).Get<AuthenticationSettings>() ?? new();

        if (settings.UseDevelopmentUser)
        {
            if (environment.IsProduction())
            {
                throw new InvalidOperationException(
                    "Authentication:UseDevelopmentUser cannot be enabled in Production.");
            }

            services.AddAuthentication(DevelopmentAuthenticationHandler.SchemeName)
                .AddScheme<DevelopmentAuthenticationOptions, DevelopmentAuthenticationHandler>(
                    DevelopmentAuthenticationHandler.SchemeName,
                    options => options.User = settings.DevelopmentUser);

            return services;
        }

        if (string.IsNullOrWhiteSpace(settings.Jwt.Authority))
        {
            throw new InvalidOperationException(
                "Authentication:Jwt:Authority is required when the development user is disabled.");
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = settings.Jwt.Authority;
                options.Audience = settings.Jwt.Audience;
                options.MapInboundClaims = false;
                options.TokenValidationParameters.RoleClaimType = settings.Jwt.RoleClaimType;
                options.TokenValidationParameters.NameClaimType = "sub";
            });

        return services;
    }

    private static IServiceCollection AddWaggoRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        RateLimitingSettings settings =
            configuration.GetSection(RateLimitingSettings.SectionName).Get<RateLimitingSettings>() ?? new();

        return services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.User.Identity?.Name
                        ?? context.Connection.RemoteIpAddress?.ToString()
                        ?? "anonymous",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = settings.PermitLimit,
                        Window = TimeSpan.FromSeconds(settings.WindowSeconds),
                        QueueLimit = 0,
                    }));
            options.OnRejected = async (context, cancellationToken) =>
                await context.HttpContext.Response.WriteAsJsonAsync(
                    WaggoApiResponseFactory.FromError(
                        ApiErrors.TooManyRequests.Code,
                        ApiErrors.TooManyRequests.Message,
                        TraceIdHelper.Get(context.HttpContext)),
                    cancellationToken);
        });
    }

    /// <summary>Role-based policies. Secure by default: every endpoint needs an authenticated user.</summary>
    private static IServiceCollection AddWaggoAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(WaggoPolicies.Owner, policy => policy.RequireRole(WaggoRoles.Owner, WaggoRoles.Admin))
            .AddPolicy(WaggoPolicies.Walker, policy => policy.RequireRole(WaggoRoles.Walker, WaggoRoles.Admin))
            .AddPolicy(WaggoPolicies.Admin, policy => policy.RequireRole(WaggoRoles.Admin))
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        return services;
    }

    /// <summary>First validation layer: request DTO validators of this assembly.</summary>
    private static IServiceCollection AddWaggoRequestValidation(this IServiceCollection services) =>
        services.AddValidatorsFromAssembly(typeof(ServiceCollectionExtensions).Assembly, includeInternalTypes: true);
}
