using FluentAssertions;
using KestrelApi.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KestrelApi.UnitTests.Controllers;

public class HealthControllerTests
{
    private readonly Mock<HealthCheckService> _healthCheckServiceMock;
    private readonly Mock<ILogger<HealthController>> _loggerMock;
    private readonly HealthController _controller;

    public HealthControllerTests()
    {
        _healthCheckServiceMock = new Mock<HealthCheckService>();
        _loggerMock = new Mock<ILogger<HealthController>>();
        _controller = new HealthController(_healthCheckServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetHealth_WhenHealthy_ShouldReturn200()
    {
        // Arrange
        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["auth0"] = new HealthReportEntry(
                    HealthStatus.Healthy,
                    "Auth0 is reachable",
                    TimeSpan.FromMilliseconds(100),
                    null,
                    null)
            },
            HealthStatus.Healthy,
            TimeSpan.FromMilliseconds(100));

        _healthCheckServiceMock
            .Setup(x => x.CheckHealthAsync(It.IsAny<Func<HealthCheckRegistration, bool>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthReport);

        // Act
        var result = await _controller.GetHealth();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        
        var response = okResult.Value.Should().BeOfType<HealthCheckResponse>().Subject;
        response.Status.Should().Be("Healthy");
        response.Entries.Should().HaveCount(1);
        response.Entries[0].Name.Should().Be("auth0");
        response.Entries[0].Status.Should().Be("Healthy");
    }

    [Fact]
    public async Task GetHealth_WhenUnhealthy_ShouldReturn503()
    {
        // Arrange
        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["auth0"] = new HealthReportEntry(
                    HealthStatus.Unhealthy,
                    "Auth0 is unreachable",
                    TimeSpan.FromMilliseconds(100),
                    new InvalidOperationException("Connection failed"),
                    null)
            },
            HealthStatus.Unhealthy,
            TimeSpan.FromMilliseconds(100));

        _healthCheckServiceMock
            .Setup(x => x.CheckHealthAsync(It.IsAny<Func<HealthCheckRegistration, bool>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthReport);

        // Act
        var result = await _controller.GetHealth();

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        
        var response = objectResult.Value.Should().BeOfType<HealthCheckResponse>().Subject;
        response.Status.Should().Be("Unhealthy");
        response.Entries[0].Exception.Should().Be("Connection failed");
    }

    [Fact]
    public async Task GetReadiness_ShouldCheckOnlyReadyTaggedChecks()
    {
        // Arrange
        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>(),
            HealthStatus.Healthy,
            TimeSpan.FromMilliseconds(50));

        Func<HealthCheckRegistration, bool>? capturedPredicate = null;
        _healthCheckServiceMock
            .Setup(x => x.CheckHealthAsync(It.IsAny<Func<HealthCheckRegistration, bool>>(), It.IsAny<CancellationToken>()))
            .Callback<Func<HealthCheckRegistration, bool>, CancellationToken>((predicate, _) => capturedPredicate = predicate)
            .ReturnsAsync(healthReport);

        // Act
        await _controller.GetReadiness();

        // Assert
        capturedPredicate.Should().NotBeNull();
        
        // Verify the predicate checks for "ready" tag
        var registrationWithReadyTag = new HealthCheckRegistration("test", instance: Mock.Of<IHealthCheck>(), null, new[] { "ready" });
        capturedPredicate!(registrationWithReadyTag).Should().BeTrue();
        
        var registrationWithLiveTag = new HealthCheckRegistration("test", instance: Mock.Of<IHealthCheck>(), null, new[] { "live" });
        capturedPredicate!(registrationWithLiveTag).Should().BeFalse();
    }

    [Fact]
    public async Task GetLiveness_ShouldCheckOnlyLiveTaggedChecks()
    {
        // Arrange
        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>(),
            HealthStatus.Healthy,
            TimeSpan.FromMilliseconds(50));

        Func<HealthCheckRegistration, bool>? capturedPredicate = null;
        _healthCheckServiceMock
            .Setup(x => x.CheckHealthAsync(It.IsAny<Func<HealthCheckRegistration, bool>>(), It.IsAny<CancellationToken>()))
            .Callback<Func<HealthCheckRegistration, bool>, CancellationToken>((predicate, _) => capturedPredicate = predicate)
            .ReturnsAsync(healthReport);

        // Act
        await _controller.GetLiveness();

        // Assert
        capturedPredicate.Should().NotBeNull();
        
        // Verify the predicate checks for "live" tag
        var registrationWithLiveTag = new HealthCheckRegistration("test", instance: Mock.Of<IHealthCheck>(), null, new[] { "live" });
        capturedPredicate!(registrationWithLiveTag).Should().BeTrue();
        
        var registrationWithReadyTag = new HealthCheckRegistration("test", instance: Mock.Of<IHealthCheck>(), null, new[] { "ready" });
        capturedPredicate!(registrationWithReadyTag).Should().BeFalse();
    }

    [Fact]
    public async Task GetHealth_WithMultipleChecks_ShouldIncludeAllInResponse()
    {
        // Arrange
        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["auth0"] = new HealthReportEntry(
                    HealthStatus.Healthy,
                    "Auth0 is reachable",
                    TimeSpan.FromMilliseconds(100),
                    null,
                    new Dictionary<string, object> { ["endpoint"] = "https://auth0.com" }),
                ["database"] = new HealthReportEntry(
                    HealthStatus.Degraded,
                    "Database is slow",
                    TimeSpan.FromMilliseconds(500),
                    null,
                    null)
            },
            HealthStatus.Degraded,
            TimeSpan.FromMilliseconds(600));

        _healthCheckServiceMock
            .Setup(x => x.CheckHealthAsync(It.IsAny<Func<HealthCheckRegistration, bool>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthReport);

        // Act
        var result = await _controller.GetHealth();

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        
        var response = objectResult.Value.Should().BeOfType<HealthCheckResponse>().Subject;
        response.Status.Should().Be("Degraded");
        response.TotalDuration.Should().Be(TimeSpan.FromMilliseconds(600));
        response.Entries.Should().HaveCount(2);
        
        var auth0Entry = response.Entries.First(e => e.Name == "auth0");
        auth0Entry.Status.Should().Be("Healthy");
        auth0Entry.Data.Should().ContainKey("endpoint");
        
        var dbEntry = response.Entries.First(e => e.Name == "database");
        dbEntry.Status.Should().Be("Degraded");
        dbEntry.Description.Should().Be("Database is slow");
    }
}