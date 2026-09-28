using System.Text.Json.Serialization;
using Waggo.Api.Endpoints;
using Waggo.Application;
using Waggo.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

builder.Services.AddOpenApi();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
          .AllowAnyHeader()
          .AllowAnyMethod()));
builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

app.UseExceptionHandler(new ExceptionHandlerOptions
{
    // Binding errors (e.g. unknown walkType) are client errors, not 500s.
    StatusCodeSelector = ex => ex is BadHttpRequestException bad ? bad.StatusCode : StatusCodes.Status500InternalServerError,
});
app.UseStatusCodePages();
app.UseCors();

app.MapOpenApi();
app.MapHealthChecks("/health");

var v1 = app.MapGroup("/api/v1");
v1.MapPricingEndpoints();

await app.RunAsync().ConfigureAwait(false);

/// <summary>Exposed for WebApplicationFactory in integration tests.</summary>
public partial class Program;
