using System.Text.Json.Serialization;
using Serilog;
using Waggo.Api.Common.Responses;
using Waggo.Api.Endpoints;
using Waggo.Application;
using Waggo.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Serilog is the only logging provider. Code logs through ILogger<T>; sinks/levels come from the "Serilog" section.
builder.Services.AddSerilog((services, configuration) => configuration
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

builder.Services.AddOpenApi();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
          .AllowAnyHeader()
          .AllowAnyMethod()));
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

// Every response — including exceptions, binding errors and unknown routes — is a WaggoApiResponse.
app.UseExceptionHandler(new ExceptionHandlerOptions { ExceptionHandler = WaggoErrorHandling.HandleExceptionAsync });
app.UseStatusCodePages(WaggoErrorHandling.HandleStatusCodeAsync);
app.UseSerilogRequestLogging();
app.UseCors();

app.MapOpenApi();
app.MapHealthChecks("/health");

var v1 = app.MapGroup("/api/v1");
v1.MapPricingEndpoints();

await app.RunAsync();

/// <summary>Exposed for WebApplicationFactory in integration tests.</summary>
public partial class Program;
