using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using HealthChecks.UI.Client;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using System.Threading.RateLimiting;

// Configure Serilog early - check if already configured for tests
if (Log.Logger == null || Log.Logger.GetType().Name == "SilentLogger")
{
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
        .Enrich.FromLogContext()
        .WriteTo.Console(formatProvider: System.Globalization.CultureInfo.InvariantCulture)
        .CreateBootstrapLogger();
}

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .Enrich.WithProperty("Application", "KestrelApi")
    .WriteTo.Console(new RenderedCompactJsonFormatter())
    .WriteTo.File(new CompactJsonFormatter(), 
        "logs/log-.json", 
        rollingInterval: RollingInterval.Day,
        rollOnFileSizeLimit: true,
        fileSizeLimitBytes: 10 * 1024 * 1024,
        retainedFileCountLimit: 30));

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add controllers
builder.Services.AddControllers();

// Add Data Protection for encryption
builder.Services.AddDataProtection();

// Add rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("api", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 10
            }));
    
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 5
            }));
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("ApiPolicy", policy =>
    {
        policy.WithOrigins("https://localhost:3000", "https://localhost:5001")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Register domain services and repositories
// Secrets domain
builder.Services.AddScoped<KestrelApi.Secrets.ISecretsService, KestrelApi.Secrets.SecretsService>();
builder.Services.AddSingleton<KestrelApi.Secrets.ISecretsRepository, KestrelApi.Secrets.InMemorySecretsRepository>();

// Names domain
builder.Services.AddScoped<KestrelApi.Names.INamesService, KestrelApi.Names.NamesService>();
builder.Services.AddSingleton<KestrelApi.Names.INamesRepository, KestrelApi.Names.InMemoryNamesRepository>();

// Security domain
builder.Services.AddScoped<KestrelApi.Security.IEncryptionService, KestrelApi.Security.DataProtectionEncryptionService>();

// Add authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Auth0:Domain"];
        options.Audience = builder.Configuration["Auth0:Audience"];
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("WriteSecrets", policy =>
        policy.Requirements.Add(new KestrelApi.Security.PermissionRequirement("write:secrets")));
});

// Register the authorization handler
builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, KestrelApi.Security.PermissionAuthorizationHandler>();

// Add health checks
builder.Services.AddHttpClient();
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["ready"])
    .AddCheck<KestrelApi.HealthChecks.Auth0HealthCheck>("auth0", tags: ["ready", "live"]);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseMiddleware<KestrelApi.Infrastructure.GlobalExceptionHandlingMiddleware>();
app.UseMiddleware<KestrelApi.Infrastructure.SecurityHeadersMiddleware>();

app.UseSerilogRequestLogging(options =>
{
    // Customize the message template
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    
    // Exclude health check endpoints from logs
    options.GetLevel = (httpContext, elapsed, ex) => 
    {
        if (httpContext.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase))
            return LogEventLevel.Verbose;
        return ex != null || httpContext.Response.StatusCode > 499 
            ? LogEventLevel.Error 
            : LogEventLevel.Information;
    };
});

app.UseRateLimiter();
app.UseCors("ApiPolicy");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

try
{
    Log.Information("Starting web application");
    app.Run();
}
catch (HostAbortedException)
{
    // Expected exception when host is stopped
    Log.Information("Host stopped");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw; // Re-throw to ensure proper exit code
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }