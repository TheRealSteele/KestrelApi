using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KestrelApi.Controllers;

[ApiController]
[Route("[controller]")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        HealthCheckService healthCheckService,
        ILogger<HealthController> logger)
    {
        _healthCheckService = healthCheckService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(HealthCheckResponse), StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(HealthCheckResponse), StatusCodes.Status503ServiceUnavailable, MediaTypeNames.Application.Json)]
    public async Task<IActionResult> GetHealth()
    {
        var report = await _healthCheckService.CheckHealthAsync();
        return report.Status == HealthStatus.Healthy ? Ok(CreateHealthCheckResponse(report)) : StatusCode(StatusCodes.Status503ServiceUnavailable, CreateHealthCheckResponse(report));
    }

    [HttpGet("ready")]
    [ProducesResponseType(typeof(HealthCheckResponse), StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(HealthCheckResponse), StatusCodes.Status503ServiceUnavailable, MediaTypeNames.Application.Json)]
    public async Task<IActionResult> GetReadiness()
    {
        var report = await _healthCheckService.CheckHealthAsync(
            predicate: check => check.Tags.Contains("ready"));
        
        return report.Status == HealthStatus.Healthy ? Ok(CreateHealthCheckResponse(report)) : StatusCode(StatusCodes.Status503ServiceUnavailable, CreateHealthCheckResponse(report));
    }

    [HttpGet("live")]
    [ProducesResponseType(typeof(HealthCheckResponse), StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(HealthCheckResponse), StatusCodes.Status503ServiceUnavailable, MediaTypeNames.Application.Json)]
    public async Task<IActionResult> GetLiveness()
    {
        var report = await _healthCheckService.CheckHealthAsync(
            predicate: check => check.Tags.Contains("live"));
        
        return report.Status == HealthStatus.Healthy ? Ok(CreateHealthCheckResponse(report)) : StatusCode(StatusCodes.Status503ServiceUnavailable, CreateHealthCheckResponse(report));
    }

    private static HealthCheckResponse CreateHealthCheckResponse(HealthReport report)
    {
        var entries = report.Entries.Select(x => new HealthCheckEntry
        {
            Name = x.Key,
            Status = x.Value.Status.ToString(),
            Description = x.Value.Description,
            Duration = x.Value.Duration,
            Tags = x.Value.Tags.ToList(),
            Data = x.Value.Data.Count > 0 ? x.Value.Data : null,
            Exception = x.Value.Exception?.Message
        }).ToList();

        var response = new HealthCheckResponse
        {
            Status = report.Status.ToString(),
            TotalDuration = report.TotalDuration,
            Entries = entries
        };

        return response;
    }
}

public class HealthCheckResponse
{
    public string Status { get; set; } = string.Empty;
    public TimeSpan TotalDuration { get; set; }
    public IReadOnlyList<HealthCheckEntry> Entries { get; init; } = new List<HealthCheckEntry>();
}

public class HealthCheckEntry
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TimeSpan Duration { get; set; }
    public IReadOnlyList<string> Tags { get; init; } = new List<string>();
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, object>? Data { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Exception { get; set; }
}