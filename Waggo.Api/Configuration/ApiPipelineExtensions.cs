using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using Waggo.Api.Common.Errors;
using Waggo.Api.Endpoints.Pricing;

namespace Waggo.Api.Configuration;

/// <summary>HTTP pipeline. The ORDER matters and is documented step by step.</summary>
public static class ApiPipelineExtensions
{
    public static WebApplication UseWaggoPipeline(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // 1. Exceptions: outermost, so it catches errors of every middleware and endpoint below.
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        // 2. Request logging (Serilog): one line per request with status and duration.
        app.UseSerilogRequestLogging();

        // 3. Empty 4xx responses (401, 403, 404, 405, 429...) get a WaggoApiResponse body.
        app.UseStatusCodePages(StatusCodeResponses.WriteAsync);

        // 4. CORS before authentication, so preflight (OPTIONS) requests are answered without a token.
        app.UseCors();

        // 5. Who is calling (JWT or development user) and 6. what they can do (roles).
        app.UseAuthentication();
        app.UseAuthorization();

        // 7. Rate limiting after authentication, so limits are per user instead of per IP.
        app.UseRateLimiter();

        return app;
    }

    public static WebApplication MapWaggoEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Health checks: public, never rate limited. Live = process up; Ready = dependencies (PostgreSQL) up.
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false })
            .AllowAnonymous()
            .DisableRateLimiting();
        app.MapHealthChecks(
                "/health/ready",
                new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") })
            .AllowAnonymous()
            .DisableRateLimiting();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi().AllowAnonymous();
        }

        RouteGroupBuilder v1 = app.MapGroup("/api/v1");
        v1.MapPricingEndpoints();

        return app;
    }
}
