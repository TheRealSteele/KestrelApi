using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net.Http;

namespace KestrelApi.HealthChecks;

public class Auth0HealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<Auth0HealthCheck> _logger;

    public Auth0HealthCheck(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<Auth0HealthCheck> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var auth0Domain = _configuration["Auth0:Domain"];
            if (string.IsNullOrEmpty(auth0Domain))
            {
                return HealthCheckResult.Unhealthy("Auth0 domain not configured");
            }

            // Remove trailing slash if present
            auth0Domain = auth0Domain.TrimEnd('/');
            
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(5);

            // Check Auth0's well-known endpoint
            var response = await httpClient.GetAsync(
                new Uri($"{auth0Domain}/.well-known/openid-configuration"),
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy("Auth0 is reachable");
            }

            return HealthCheckResult.Unhealthy(
                $"Auth0 returned status code: {response.StatusCode}");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to reach Auth0");
            return HealthCheckResult.Unhealthy("Failed to reach Auth0", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Auth0 health check timed out");
            return HealthCheckResult.Unhealthy("Auth0 health check timed out", ex);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation during Auth0 health check");
            return HealthCheckResult.Unhealthy("Invalid operation", ex);
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Invalid argument during Auth0 health check");
            return HealthCheckResult.Unhealthy("Invalid configuration", ex);
        }
    }
}