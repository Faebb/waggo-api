using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Waggo.Api.Infrastructure.Authentication;
using Waggo.Api.Infrastructure.Authorization;
using Waggo.Api.Infrastructure.Errors;
using Waggo.Api.Infrastructure.Helpers;
using Waggo.Api.Infrastructure.Responses;
using Waggo.Api.Infrastructure.Settings;
using Waggo.Application.Common.Constants;
using Waggo.Application.Common.Interfaces;

namespace Waggo.Api.Infrastructure.Extensions;

/// <summary>Registers every API service in the DI container. Each concern has its own method.</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWaggoApi(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        EmailStartupGuard.EnsureRealProviderInProduction(configuration, environment);

        return services
            .AddWaggoLogging(configuration)
            .AddWaggoJson()
            .AddWaggoCors(configuration)
            .AddWaggoAuthentication(configuration, environment)
            .AddWaggoAuthorization()
            .AddWaggoRateLimiting(configuration)
            .AddWaggoRequestValidation()
            .AddWaggoCurrentUser()
            .AddOpenApi();
    }

    /// <summary>Use cases get the caller through <see cref="ICurrentUser"/>, never from HttpContext.</summary>
    private static IServiceCollection AddWaggoCurrentUser(this IServiceCollection services) =>
        services
            .AddHttpContextAccessor()
            .AddScoped<ICurrentUser, HttpCurrentUser>();

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
    /// ADR-016: the API validates the JWT access tokens it issues itself, with its own symmetric key — not an
    /// external provider's OIDC metadata. Development can use a fake user instead (never in Production).
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

        if (string.IsNullOrWhiteSpace(settings.Jwt.SigningKey))
        {
            throw new InvalidOperationException(
                "Authentication:Jwt:SigningKey is required when the development user is disabled.");
        }

        byte[] key = Convert.FromBase64String(settings.Jwt.SigningKey);
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = settings.Jwt.Issuer,
                    ValidAudience = settings.Jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    RoleClaimType = settings.Jwt.RoleClaimType,
                    NameClaimType = "sub",
                };
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
            .AddPolicy(WaggoPolicies.Authenticated, policy => policy.RequireAuthenticatedUser())
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
