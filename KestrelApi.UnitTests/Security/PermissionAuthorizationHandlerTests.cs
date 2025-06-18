using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using KestrelApi.Security;

namespace KestrelApi.UnitTests.Security;

public class PermissionAuthorizationHandlerTests
{
    private readonly PermissionAuthorizationHandler _sut;

    public PermissionAuthorizationHandlerTests()
    {
        _sut = new PermissionAuthorizationHandler();
    }

    [Fact]
    public async Task HandleRequirementAsync_WithMatchingPermissionInSingleClaim_ShouldSucceed()
    {
        // Arrange
        var requirement = new PermissionRequirement("write:secrets");
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("permissions", "read:secrets,write:secrets,read:names")
        }));
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null);

        // Act
        await _sut.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirementAsync_WithMatchingPermissionInMultipleClaims_ShouldSucceed()
    {
        // Arrange
        var requirement = new PermissionRequirement("write:secrets");
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("permissions", "read:secrets"),
            new Claim("permissions", "write:secrets"),
            new Claim("permissions", "read:names")
        }));
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null);

        // Act
        await _sut.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirementAsync_WithoutMatchingPermission_ShouldNotSucceed()
    {
        // Arrange
        var requirement = new PermissionRequirement("write:secrets");
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("permissions", "read:secrets,read:names")
        }));
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null);

        // Act
        await _sut.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeFalse();
        context.HasFailed.Should().BeFalse(); // Should not explicitly fail, just not succeed
    }

    [Fact]
    public async Task HandleRequirementAsync_WithNoPermissionsClaim_ShouldNotSucceed()
    {
        // Arrange
        var requirement = new PermissionRequirement("write:secrets");
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", "user123"),
            new Claim("email", "user@example.com")
        }));
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null);

        // Act
        await _sut.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirementAsync_WithEmptyPermissionsClaim_ShouldNotSucceed()
    {
        // Arrange
        var requirement = new PermissionRequirement("write:secrets");
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("permissions", "")
        }));
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null);

        // Act
        await _sut.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirementAsync_WithWhitespaceInPermissions_ShouldHandleCorrectly()
    {
        // Arrange
        var requirement = new PermissionRequirement("write:secrets");
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("permissions", "read:secrets, write:secrets , read:names")
        }));
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null);

        // Act
        await _sut.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeTrue();
    }

    [Theory]
    [InlineData("write:secrets", "write:secrets", true)]
    [InlineData("write:secrets", "read:secrets", false)]
    [InlineData("admin", "admin", true)]
    [InlineData("admin", "user", false)]
    public async Task HandleRequirementAsync_WithVariousPermissions_ShouldHandleCorrectly(
        string requiredPermission,
        string userPermission,
        bool shouldSucceed)
    {
        // Arrange
        var requirement = new PermissionRequirement(requiredPermission);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("permissions", userPermission)
        }));
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null);

        // Act
        await _sut.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().Be(shouldSucceed);
    }
}

public class PermissionRequirementTests
{
    [Fact]
    public void Constructor_ShouldSetPermissionProperty()
    {
        // Arrange & Act
        var requirement = new PermissionRequirement("write:secrets");

        // Assert
        requirement.Permission.Should().Be("write:secrets");
    }
}