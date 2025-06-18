using System.Security.Cryptography;
using FluentAssertions;
using KestrelApi.Secrets;
using KestrelApi.Security;
using Microsoft.Extensions.Logging;
using Moq;

namespace KestrelApi.UnitTests.Services;

public class SecretsServiceTests
{
    private readonly Mock<ISecretsRepository> _repositoryMock;
    private readonly Mock<IEncryptionService> _encryptionServiceMock;
    private readonly Mock<ILogger<SecretsService>> _loggerMock;
    private readonly SecretsService _sut;

    public SecretsServiceTests()
    {
        _repositoryMock = new Mock<ISecretsRepository>();
        _encryptionServiceMock = new Mock<IEncryptionService>();
        _loggerMock = new Mock<ILogger<SecretsService>>();
        _sut = new SecretsService(_repositoryMock.Object, _encryptionServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task AddSecretAsync_WithValidInput_ShouldEncryptAndStoreSecret()
    {
        var userId = "user123";
        var secret = "my-secret-value";
        var encryptedSecret = "encrypted-secret";
        var expectedId = "secret-id-123";

        _encryptionServiceMock
            .Setup(x => x.EncryptAsync(secret))
            .ReturnsAsync(encryptedSecret);

        _repositoryMock
            .Setup(x => x.Add(userId, encryptedSecret))
            .Returns(expectedId);

        var result = await _sut.AddSecretAsync(userId, secret);

        result.Should().Be(expectedId);
        _encryptionServiceMock.Verify(x => x.EncryptAsync(secret), Times.Once);
        _repositoryMock.Verify(x => x.Add(userId, encryptedSecret), Times.Once);
    }

    [Fact]
    public async Task AddSecretAsync_WhenArgumentNullExceptionThrown_ShouldRethrow()
    {
        var userId = "user123";
        var secret = "my-secret";

        _encryptionServiceMock
            .Setup(x => x.EncryptAsync(It.IsAny<string>()))
            .ThrowsAsync(new ArgumentNullException());

        var act = async () => await _sut.AddSecretAsync(userId, secret);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task AddSecretAsync_WhenCryptographicExceptionThrown_ShouldRethrow()
    {
        var userId = "user123";
        var secret = "my-secret";

        _encryptionServiceMock
            .Setup(x => x.EncryptAsync(It.IsAny<string>()))
            .ThrowsAsync(new CryptographicException());

        var act = async () => await _sut.AddSecretAsync(userId, secret);

        await act.Should().ThrowAsync<CryptographicException>();
    }

    [Fact]
    public async Task AddSecretAsync_WhenInvalidOperationExceptionThrown_ShouldRethrow()
    {
        var userId = "user123";
        var secret = "my-secret";
        var encryptedSecret = "encrypted";

        _encryptionServiceMock
            .Setup(x => x.EncryptAsync(secret))
            .ReturnsAsync(encryptedSecret);

        _repositoryMock
            .Setup(x => x.Add(userId, encryptedSecret))
            .Throws(new InvalidOperationException());

        var act = async () => await _sut.AddSecretAsync(userId, secret);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetSecretsAsync_WithValidUserId_ShouldReturnDecryptedSecrets()
    {
        var userId = "user123";
        var encryptedSecrets = new[] { "encrypted1", "encrypted2", "encrypted3" };
        var decryptedSecrets = new[] { "secret1", "secret2", "secret3" };

        _repositoryMock
            .Setup(x => x.GetByUserId(userId))
            .Returns(encryptedSecrets);

        _encryptionServiceMock
            .Setup(x => x.DecryptAsync("encrypted1"))
            .ReturnsAsync("secret1");
        _encryptionServiceMock
            .Setup(x => x.DecryptAsync("encrypted2"))
            .ReturnsAsync("secret2");
        _encryptionServiceMock
            .Setup(x => x.DecryptAsync("encrypted3"))
            .ReturnsAsync("secret3");

        var result = await _sut.GetSecretsAsync(userId);

        result.Should().BeEquivalentTo(decryptedSecrets);
        _repositoryMock.Verify(x => x.GetByUserId(userId), Times.Once);
        _encryptionServiceMock.Verify(x => x.DecryptAsync(It.IsAny<string>()), Times.Exactly(3));
    }

    [Fact]
    public async Task GetSecretsAsync_WithNoSecrets_ShouldReturnEmptyCollection()
    {
        var userId = "user123";
        var encryptedSecrets = Array.Empty<string>();

        _repositoryMock
            .Setup(x => x.GetByUserId(userId))
            .Returns(encryptedSecrets);

        var result = await _sut.GetSecretsAsync(userId);

        result.Should().BeEmpty();
        _encryptionServiceMock.Verify(x => x.DecryptAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetSecretsAsync_WhenArgumentNullExceptionThrown_ShouldRethrow()
    {
        var userId = "user123";

        _repositoryMock
            .Setup(x => x.GetByUserId(It.IsAny<string>()))
            .Throws(new ArgumentNullException());

        var act = async () => await _sut.GetSecretsAsync(userId);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetSecretsAsync_WhenCryptographicExceptionThrown_ShouldRethrow()
    {
        var userId = "user123";
        var encryptedSecrets = new[] { "encrypted1" };

        _repositoryMock
            .Setup(x => x.GetByUserId(userId))
            .Returns(encryptedSecrets);

        _encryptionServiceMock
            .Setup(x => x.DecryptAsync(It.IsAny<string>()))
            .ThrowsAsync(new CryptographicException());

        var act = async () => await _sut.GetSecretsAsync(userId);

        await act.Should().ThrowAsync<CryptographicException>();
    }

    [Fact]
    public async Task GetSecretsAsync_WhenInvalidOperationExceptionThrown_ShouldRethrow()
    {
        var userId = "user123";

        _repositoryMock
            .Setup(x => x.GetByUserId(userId))
            .Throws(new InvalidOperationException());

        var act = async () => await _sut.GetSecretsAsync(userId);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}