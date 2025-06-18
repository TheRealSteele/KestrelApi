using FluentAssertions;
using KestrelApi.Names;
using Microsoft.Extensions.Logging;
using Moq;

namespace KestrelApi.UnitTests.Services;

public class NamesServiceTests
{
    private readonly Mock<INamesRepository> _repositoryMock;
    private readonly Mock<ILogger<NamesService>> _loggerMock;
    private readonly NamesService _sut;

    public NamesServiceTests()
    {
        _repositoryMock = new Mock<INamesRepository>();
        _loggerMock = new Mock<ILogger<NamesService>>();
        _sut = new NamesService(_repositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task AddNameAsync_WithValidInput_ShouldAddName()
    {
        var userId = "user123";
        var name = "John Doe";
        var expectedId = "name-id-123";

        _repositoryMock
            .Setup(x => x.Add(userId, name))
            .Returns(expectedId);

        var result = await _sut.AddNameAsync(userId, name);

        result.Should().Be(expectedId);
        _repositoryMock.Verify(x => x.Add(userId, name), Times.Once);
    }

    [Fact]
    public async Task AddNameAsync_WhenArgumentNullExceptionThrown_ShouldRethrow()
    {
        var userId = "user123";
        var name = "John Doe";

        _repositoryMock
            .Setup(x => x.Add(It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new ArgumentNullException());

        var act = async () => await _sut.AddNameAsync(userId, name);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task AddNameAsync_WhenInvalidOperationExceptionThrown_ShouldRethrow()
    {
        var userId = "user123";
        var name = "John Doe";

        _repositoryMock
            .Setup(x => x.Add(userId, name))
            .Throws(new InvalidOperationException());

        var act = async () => await _sut.AddNameAsync(userId, name);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetNamesAsync_WithValidUserId_ShouldReturnNames()
    {
        var userId = "user123";
        var names = new[] { "John Doe", "Jane Smith", "Bob Johnson" };

        _repositoryMock
            .Setup(x => x.GetByUserId(userId))
            .Returns(names);

        var result = await _sut.GetNamesAsync(userId);

        result.Should().BeEquivalentTo(names);
        _repositoryMock.Verify(x => x.GetByUserId(userId), Times.Once);
    }

    [Fact]
    public async Task GetNamesAsync_WithNoNames_ShouldReturnEmptyCollection()
    {
        var userId = "user123";
        var names = Array.Empty<string>();

        _repositoryMock
            .Setup(x => x.GetByUserId(userId))
            .Returns(names);

        var result = await _sut.GetNamesAsync(userId);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetNamesAsync_WhenArgumentNullExceptionThrown_ShouldRethrow()
    {
        var userId = "user123";

        _repositoryMock
            .Setup(x => x.GetByUserId(It.IsAny<string>()))
            .Throws(new ArgumentNullException());

        var act = async () => await _sut.GetNamesAsync(userId);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetNamesAsync_WhenInvalidOperationExceptionThrown_ShouldRethrow()
    {
        var userId = "user123";

        _repositoryMock
            .Setup(x => x.GetByUserId(userId))
            .Throws(new InvalidOperationException());

        var act = async () => await _sut.GetNamesAsync(userId);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}