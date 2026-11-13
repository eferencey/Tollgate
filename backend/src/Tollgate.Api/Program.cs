// Entry point and composition root of the Tollgate backend.
using Tollgate.Api.Configuration;
using Tollgate.Api.Endpoints;
using Tollgate.Api.Errors;
using Tollgate.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddDotEnvFile(builder.Environment.ContentRootPath).MapProviderKeys();
var mode = builder.Configuration.ResolveScanMode();

builder.Services.AddTollgateBackend(builder.Configuration, mode);
builder.Services.AddTollgateWeb();

var app = builder.Build();

// Creates the SQLite file on first start and applies pending migrations (the schema is part of the code).
await app.Services.MigrateDatabaseAsync();

app.UseMiddleware<ClientDisconnectMiddleware>(); // outermost: a client that left is not an error
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseRateLimiter();

app.MapOpenApi();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Tollgate API v1"));
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
app.MapScanEndpoints();
app.MapHistoryEndpoints();

app.Logger.LogInformation("Tollgate started in {Mode} mode", mode);
app.Run();

// Lets the integration tests start the app in memory (WebApplicationFactory<Program>).
public partial class Program;
