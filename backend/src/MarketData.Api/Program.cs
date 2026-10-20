using MarketData.Api.Cors;
using MarketData.Api.ErrorHandling;
using MarketData.Api.Json;
using MarketData.Api.OpenApi;
using MarketData.Api.RateLimiting;
using MarketData.Application;
using MarketData.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure();

builder.Services.AddControllers().AddApiJsonOptions();
builder.Services.AddApiErrorHandling();
builder.Services.AddApiCors();
builder.Services.AddApiRateLimiting();
builder.Services.AddApiOpenApi();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseCors(ApiCorsExtensions.FrontendPolicy);
app.UseRateLimiter();

app.MapControllers().RequireRateLimiting(ApiRateLimitingExtensions.PerClientPolicy);

// Liveness runs no checks: it only shows the process is serving requests.
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

await app.RunAsync();
