using System.Security.Claims;
using FluentAssertions;
using KestrelApi.Names;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;

namespace KestrelApi.UnitTests.Controllers;

public class NamesControllerTests
{
    private readonly Mock<INamesService> _serviceMock;
    private readonly Mock<ILogger<NamesController>> _loggerMock;
    private readonly NamesController _sut;
    private readonly ClaimsPrincipal _userWithId;
    private readonly ClaimsPrincipal _userWithoutId;

    public NamesControllerTests()
    {
        _serviceMock = new Mock<INamesService>();
        _loggerMock = new Mock<ILogger<NamesController>>();
        _sut = new NamesController(_serviceMock.Object, _loggerMock.Object);

        _userWithId = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user123")
        }, "test"));

        _userWithoutId = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { }, "test"));
    }

    [Fact]
    public async Task AddName_WithNullRequest_ShouldThrowArgumentNullException()
    {
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _userWithId }
        };

        var act = async () => await _sut.AddName(null!);

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithMessage("Value cannot be null. (Parameter 'request')");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AddName_WithInvalidName_ShouldReturnValidationProblem(string? invalidName)
    {
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _userWithId }
        };
        _sut.ModelState.AddModelError("Name", "Name is required");

        var request = new NameRequest { Name = invalidName! };

        var result = await _sut.AddName(request);

        result.Should().BeOfType<ObjectResult>()
            .Which.Value.Should().BeOfType<ValidationProblemDetails>();
    }

    [Fact]
    public async Task AddName_WithoutUserId_ShouldReturnBadRequest()
    {
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _userWithoutId }
        };

        var request = new NameRequest { Name = "John Doe" };

        var result = await _sut.AddName(request);

        result.Should().BeOfType<ObjectResult>()
            .Which.Value.Should().BeOfType<ProblemDetails>()
            .Which.Detail.Should().Be("User ID not found in token");
    }

    [Fact]
    public async Task AddName_WithValidRequest_ShouldReturnCreated()
    {
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _userWithId }
        };

        var request = new NameRequest { Name = "John Doe" };
        _serviceMock
            .Setup(x => x.AddNameAsync("user123", "John Doe"))
            .ReturnsAsync("name-id");

        var result = await _sut.AddName(request);

        result.Should().BeOfType<CreatedResult>()
            .Which.Location.Should().Be("/names");
        _serviceMock.Verify(x => x.AddNameAsync("user123", "John Doe"), Times.Once);
    }

    [Fact]
    public async Task AddName_WhenServiceThrowsArgumentNullException_ShouldReturnBadRequest()
    {
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _userWithId }
        };

        var request = new NameRequest { Name = "John Doe" };
        _serviceMock
            .Setup(x => x.AddNameAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new ArgumentNullException());

        var result = await _sut.AddName(request);

        result.Should().BeOfType<ObjectResult>()
            .Which.Value.Should().BeOfType<ProblemDetails>()
            .Which.Detail.Should().Be("Invalid request data");
    }

    [Fact]
    public async Task AddName_WhenServiceThrowsInvalidOperationException_ShouldReturn500()
    {
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _userWithId }
        };

        var request = new NameRequest { Name = "John Doe" };
        _serviceMock
            .Setup(x => x.AddNameAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException());

        var result = await _sut.AddName(request);

        result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(500);
        result.As<ObjectResult>().Value.Should().BeOfType<ProblemDetails>()
            .Which.Detail.Should().Be("An error occurred while processing your request");
    }

    [Fact]
    public async Task GetNames_WithoutUserId_ShouldReturnBadRequest()
    {
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _userWithoutId }
        };

        var result = await _sut.GetNames();

        result.Should().BeOfType<ObjectResult>()
            .Which.Value.Should().BeOfType<ProblemDetails>()
            .Which.Detail.Should().Be("User ID not found in token");
    }

    [Fact]
    public async Task GetNames_WithValidUserId_ShouldReturnNames()
    {
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _userWithId }
        };

        var names = new[] { "John Doe", "Jane Smith", "Bob Johnson" };
        _serviceMock
            .Setup(x => x.GetNamesAsync("user123"))
            .ReturnsAsync(names);

        var result = await _sut.GetNames();

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(names);
        _serviceMock.Verify(x => x.GetNamesAsync("user123"), Times.Once);
    }

    [Fact]
    public async Task GetNames_WhenServiceThrowsArgumentNullException_ShouldReturnBadRequest()
    {
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _userWithId }
        };

        _serviceMock
            .Setup(x => x.GetNamesAsync(It.IsAny<string>()))
            .ThrowsAsync(new ArgumentNullException());

        var result = await _sut.GetNames();

        result.Should().BeOfType<ObjectResult>()
            .Which.Value.Should().BeOfType<ProblemDetails>()
            .Which.Detail.Should().Be("Invalid request data");
    }

    [Fact]
    public async Task GetNames_WhenServiceThrowsInvalidOperationException_ShouldReturn500()
    {
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _userWithId }
        };

        _serviceMock
            .Setup(x => x.GetNamesAsync(It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException());

        var result = await _sut.GetNames();

        result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(500);
        result.As<ObjectResult>().Value.Should().BeOfType<ProblemDetails>()
            .Which.Detail.Should().Be("An error occurred while processing your request");
    }
}