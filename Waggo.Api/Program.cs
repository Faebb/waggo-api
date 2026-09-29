using Waggo.Api.Infrastructure.Extensions;
using Waggo.Application;
using Waggo.Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddWaggoApi(builder.Configuration, builder.Environment);

WebApplication app = builder.Build();

app.UseWaggoPipeline();
app.MapWaggoEndpoints();

await app.RunAsync();

/// <summary>Exposed for WebApplicationFactory in integration tests.</summary>
public partial class Program;
