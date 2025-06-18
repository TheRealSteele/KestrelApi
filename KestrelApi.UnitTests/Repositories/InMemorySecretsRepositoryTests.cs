using FluentAssertions;
using KestrelApi.Secrets;

namespace KestrelApi.UnitTests.Repositories;

public class InMemorySecretsRepositoryTests
{
    private readonly InMemorySecretsRepository _sut;

    public InMemorySecretsRepositoryTests()
    {
        _sut = new InMemorySecretsRepository();
    }

    [Fact]
    public async Task AddAsync_ShouldStoreEncryptedSecretAndReturnIt()
    {
        var userId = "user123";
        var encryptedSecret = "encrypted-secret-data";

        var result = await _sut.AddAsync(userId, encryptedSecret);

        result.Should().Be(encryptedSecret);
    }

    [Fact]
    public async Task AddAsync_ShouldStoreMultipleSecretsForSameUser()
    {
        var userId = "user123";
        var secrets = new[] { "encrypted1", "encrypted2", "encrypted3" };

        foreach (var secret in secrets)
        {
            await _sut.AddAsync(userId, secret);
        }

        var storedSecrets = await _sut.GetByUserIdAsync(userId);
        storedSecrets.Should().BeEquivalentTo(secrets);
    }

    [Fact]
    public async Task AddAsync_ShouldStoreSecretsSeparatelyForDifferentUsers()
    {
        var user1 = "user123";
        var user2 = "user456";
        var secret1 = "encrypted-secret-1";
        var secret2 = "encrypted-secret-2";

        await _sut.AddAsync(user1, secret1);
        await _sut.AddAsync(user2, secret2);

        var user1Secrets = await _sut.GetByUserIdAsync(user1);
        var user2Secrets = await _sut.GetByUserIdAsync(user2);

        user1Secrets.Should().BeEquivalentTo(new[] { secret1 });
        user2Secrets.Should().BeEquivalentTo(new[] { secret2 });
    }

    [Fact]
    public async Task GetByUserIdAsync_WithNoSecretsStored_ShouldReturnEmptyCollection()
    {
        var userId = "nonexistent";

        var result = await _sut.GetByUserIdAsync(userId);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnAllSecretsForUser()
    {
        var userId = "user123";
        var secrets = new[] { "encrypted1", "encrypted2", "encrypted3" };

        foreach (var secret in secrets)
        {
            await _sut.AddAsync(userId, secret);
        }

        var result = await _sut.GetByUserIdAsync(userId);

        result.Should().BeEquivalentTo(secrets);
    }

    [Fact]
    public async Task Repository_ShouldBeThreadSafe()
    {
        var userId = "user123";
        var tasks = new List<Task>();

        for (int i = 0; i < 100; i++)
        {
            var secret = $"encrypted-secret-{i}";
            tasks.Add(Task.Run(async () => await _sut.AddAsync(userId, secret)));
        }

        await Task.WhenAll(tasks);

        var storedSecrets = await _sut.GetByUserIdAsync(userId);
        storedSecrets.Should().HaveCount(100);
    }

    [Fact]
    public async Task AddAsync_WithEmptyUserId_ShouldStoreSuccessfully()
    {
        var userId = "";
        var encryptedSecret = "encrypted-data";

        var result = await _sut.AddAsync(userId, encryptedSecret);

        result.Should().Be(encryptedSecret);
        var storedSecrets = await _sut.GetByUserIdAsync(userId);
        storedSecrets.Should().ContainSingle().Which.Should().Be(encryptedSecret);
    }

    [Fact]
    public async Task AddAsync_WithEmptyEncryptedSecret_ShouldStoreSuccessfully()
    {
        var userId = "user123";
        var encryptedSecret = "";

        var result = await _sut.AddAsync(userId, encryptedSecret);

        result.Should().Be(encryptedSecret);
        var storedSecrets = await _sut.GetByUserIdAsync(userId);
        storedSecrets.Should().ContainSingle().Which.Should().Be(encryptedSecret);
    }

    [Fact]
    public async Task AddAsync_ShouldMaintainOrderOfSecrets()
    {
        var userId = "user123";
        var secrets = new[] { "first", "second", "third", "fourth", "fifth" };

        foreach (var secret in secrets)
        {
            await _sut.AddAsync(userId, secret);
        }

        var storedSecrets = await _sut.GetByUserIdAsync(userId);
        
        storedSecrets.Should().HaveCount(5);
        storedSecrets.Should().BeEquivalentTo(secrets);
    }
}