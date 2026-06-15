using JF.AgenticEnterprise.Api.Endpoints;
using JF.AgenticEnterprise.Api.Hubs;
using JF.AgenticEnterprise.Api.SignalR;
using JF.AgenticEnterprise.Application.SignalR;
using JF.AgenticEnterprise.Infrastructure;
using JF.AgenticEnterprise.Infrastructure.Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog ───────────────────────────────────────────────────────────────────
builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext());

// ── Infrastructure (EF, repositories, SK kernel, agents, orchestrator) ────────
builder.Services.AddInfrastructure(builder.Configuration);

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(opts =>
    opts.AddDefaultPolicy(p => p
        .WithOrigins("http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));

// ── SignalR + event broadcaster ───────────────────────────────────────────────
builder.Services.AddSignalR();
builder.Services.AddScoped<IAgentEventBroadcaster, SignalRAgentEventBroadcaster>();

// ── Health checks + Problem details ──────────────────────────────────────────
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();

// ── JSON ──────────────────────────────────────────────────────────────────────
builder.Services.ConfigureHttpJsonOptions(opts =>
{
    opts.SerializerOptions.PropertyNamingPolicy =
        System.Text.Json.JsonNamingPolicy.CamelCase;
    opts.SerializerOptions.DefaultIgnoreCondition =
        System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});

var app = builder.Build();

// ── Seed database ─────────────────────────────────────────────────────────────
Directory.CreateDirectory("Data");
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
    await seeder.SeedAsync();
}

// ── Middleware ────────────────────────────────────────────────────────────────
app.UseSerilogRequestLogging();
app.UseCors();
app.UseExceptionHandler();
app.UseStatusCodePages();

// ── Endpoints + Hubs ─────────────────────────────────────────────────────────
app.MapHealthChecks("/health");
app.MapHub<InboxHub>("/hubs/inbox");

app.MapEmailEndpoints();
app.MapWorkflowEndpoints();
app.MapWorkflowExecutionEndpoints();

// Sprint 3
app.MapHumanReviewEndpoints();
app.MapTaxonomyEndpoints();
app.MapWorkflowReasoningEndpoints();
app.MapDashboardEndpoints();

app.Run();
