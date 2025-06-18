using System.Security.Cryptography;
using FluentAssertions;
using KestrelApi.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Moq;

namespace KestrelApi.UnitTests.Infrastructure;

public class DataProtectionEncryptionServiceTests
{
    private readonly Mock<IDataProtectionProvider> _providerMock;
    private readonly Mock<ILogger<DataProtectionEncryptionService>> _loggerMock;

    public DataProtectionEncryptionServiceTests()
    {
        _providerMock = new Mock<IDataProtectionProvider>();
        _loggerMock = new Mock<ILogger<DataProtectionEncryptionService>>();
    }

    [Fact]
    public void Constructor_WithNullProvider_ShouldThrowArgumentNullException()
    {
        var act = () => new DataProtectionEncryptionService(null!, _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithValidProvider_ShouldCreateProtector()
    {
        var protectorMock = new Mock<IDataProtector>();
        _providerMock
            .Setup(x => x.CreateProtector("SecretProtection"))
            .Returns(protectorMock.Object);

        var service = new DataProtectionEncryptionService(_providerMock.Object, _loggerMock.Object);

        _providerMock.Verify(x => x.CreateProtector("SecretProtection"), Times.Once);
    }
}