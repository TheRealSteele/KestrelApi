using System.Security.Claims;
using System.Security.Cryptography;
using FluentAssertions;
using KestrelApi.Secrets;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace KestrelApi.UnitTests.Controllers;

public class SecretsControllerTests
{
    private readonly Mock<ISecretsService> _serviceMock;
    private readonly Mock<ILogger<SecretsController>> _loggerMock;
    private readonly SecretsController _sut;
    private readonly ClaimsPrincipal _userWithId;
    private readonly ClaimsPrincipal _userWithoutId;

    public SecretsControllerTests()
    {
        _serviceMock = new Mock<ISecretsService>();
        _loggerMock = new Mock<ILogger<SecretsController>>();
        _sut = new SecretsController(_serviceMock.Object, _loggerMock.Object);

        _userWithId = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user123")
        }, "test"));

        _userWithoutId = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { }, "test"));
    }

    [Fact]
    public async Task AddSecret_WithNullRequest_ShouldReturnBadRequest()
    {
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _userWithId }
        };

        var result = await _sut.AddSecret(null!);

        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.Should().Be("Request cannot be null");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AddSecret_WithInvalidSecret_ShouldReturnBadRequest(string? invalidSecret)
    {
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _userWithId }
        };

        var request = new SecretRequest(invalidSecret!);

        var result = await _sut.AddSecret(request);

        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.Should().Be("Secret cannot be null or empty");
    }

    [Fact]
    public async Task AddSecret_WithoutUserId_ShouldReturnBadRequest()
    {
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _userWithoutId }
        };

        var request = new SecretRequest("my-secret");

        var result = await _sut.AddSecret(request);

        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.Should().Be("User ID not found in token");
    }

    [Fact]
    public async Task AddSecret_WithValidRequest_ShouldReturnCreated()
    {
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _userWithId }
        };

        var request = new SecretRequest("my-secret");
        _serviceMock
            .Setup(x => x.AddSecretAsync("user123", "my-secret"))
            .ReturnsAsync("secret-id");

        var result = await _sut.AddSecret(request);

        result.Should().BeOfType<CreatedResult>()
            .Which.Location.Should().Be("/secrets");
        _serviceMock.Verify(x => x.AddSecretAsync("user123", "my-secret"), Times.Once);
    }

    [Fact]
    public async Task AddSecret_WhenServiceThrowsArgumentNullException_ShouldReturnBadRequest()
    {
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _userWithId }
        };

        var request = new SecretRequest("my-secret");
        _serviceMock
            .Setup(x => x.AddSecretAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new ArgumentNullException());

        var result = await _sut.AddSecret(request);

        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.Should().Be("Invalid request data");
    }

    [Fact]
    public async Task AddSecret_WhenServiceThrowsCryptographicException_ShouldReturn500()
    {
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _userWithId }
        };

        var request = new SecretRequest("my-secret");
        _serviceMock
            .Setup(x => x.AddSecretAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new CryptographicException());

        var result = await _sut.AddSecret(request);

        result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(500);
        result.As<ObjectResult>().Value.Should().Be("Failed to encrypt secret");
    }

    [Fact]
    public async Task AddSecret_WhenServiceThrowsInvalidOperationException_ShouldReturn500()
    {
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _userWithId }
        };

        var request = new SecretRequest("my-secret");
        _serviceMock
            .Setup(x => x.AddSecretAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException());

        var result = await _sut.AddSecret(request);

        result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(500);
        result.As<ObjectResult>().Value.Should().Be("An error occurred while storing the secret");
    }

    [Fact]
    public async Task GetSecrets_WithoutUserId_ShouldReturnBadRequest()
    {
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _userWithoutId }
        };

        var result = await _sut.GetSecrets();

        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.Should().Be("User ID not found in token");
    }

    [Fact]
    public async Task GetSecrets_WithValidUserId_ShouldReturnSecrets()
    {
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _userWithId }
        };

        var secrets = new[] { "secret1", "secret2", "secret3" };
        _serviceMock
            .Setup(x => x.GetSecretsAsync("user123"))
            .ReturnsAsync(secrets);

        var result = await _sut.GetSecrets();

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(secrets);
        _serviceMock.Verify(x => x.GetSecretsAsync("user123"), Times.Once);
    }

    [Fact]
    public async Task GetSecrets_WhenServiceThrowsArgumentNullException_ShouldReturnBadRequest()
    {
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _userWithId }
        };

        _serviceMock
            .Setup(x => x.GetSecretsAsync(It.IsAny<string>()))
            .ThrowsAsync(new ArgumentNullException());

        var result = await _sut.GetSecrets();

        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.Should().Be("Invalid request");
    }

    [Fact]
    public async Task GetSecrets_WhenServiceThrowsCryptographicException_ShouldReturn500()
    {
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _userWithId }
        };

        _serviceMock
            .Setup(x => x.GetSecretsAsync(It.IsAny<string>()))
            .ThrowsAsync(new CryptographicException());

        var result = await _sut.GetSecrets();

        result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(500);
        result.As<ObjectResult>().Value.Should().Be("Failed to decrypt secrets");
    }

    [Fact]
    public async Task GetSecrets_WhenServiceThrowsInvalidOperationException_ShouldReturn500()
    {
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _userWithId }
        };

        _serviceMock
            .Setup(x => x.GetSecretsAsync(It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException());

        var result = await _sut.GetSecrets();

        result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(500);
        result.As<ObjectResult>().Value.Should().Be("An error occurred while retrieving secrets");
    }
}