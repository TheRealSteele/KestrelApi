using System.Linq;
using System.Net.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;

namespace KestrelApi.IntegrationTests.Infrastructure;

public class KestrelApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Remove the existing JWT Bearer authentication
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IAuthenticationSchemeProvider));
            
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Remove JWT Bearer specific services
            services.RemoveAll(typeof(IPostConfigureOptions<JwtBearerOptions>));

            // Add test authentication
            services.AddAuthentication(TestAuthHandler.AuthenticationScheme)
                .AddScheme<TestAuthHandlerOptions, TestAuthHandler>(
                    TestAuthHandler.AuthenticationScheme, 
                    options => { });

            // Re-add authorization services
            services.AddAuthorization();
            
            // Mock HttpClient for Auth0 health check
            services.AddSingleton<IHttpClientFactory>(new TestHttpClientFactory());
        });

        builder.UseEnvironment("Test");
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        
        // Configure test logging first to prevent conflicts
        var testLogger = new LoggerConfiguration()
            .MinimumLevel.Warning() // Reduce noise in tests
            .MinimumLevel.Override("Microsoft", LogEventLevel.Error)
            .MinimumLevel.Override("System", LogEventLevel.Error)
            .WriteTo.Console(formatProvider: System.Globalization.CultureInfo.InvariantCulture)
            .CreateLogger();
            
        Log.Logger = testLogger;
        
        // Configure test-specific settings
        builder.ConfigureHostConfiguration(config =>
        {
            // Add test Auth0 configuration
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth0:Domain"] = "https://test-tenant.auth0.com/",
                ["Auth0:Audience"] = "https://test-api-identifier",
                // Override Serilog settings for tests
                ["Serilog:MinimumLevel:Default"] = "Warning",
                ["Serilog:MinimumLevel:Override:Microsoft"] = "Error",
                ["Serilog:MinimumLevel:Override:System"] = "Error"
            });
        });

        // Configure logging for tests
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddSerilog(testLogger, dispose: false);
        });

        return base.CreateHost(builder);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Clean up Serilog to prevent conflicts between tests
            Log.CloseAndFlush();
        }
        
        base.Dispose(disposing);
    }
}