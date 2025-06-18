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
    public void Add_ShouldStoreEncryptedSecretAndReturnIt()
    {
        var userId = "user123";
        var encryptedSecret = "encrypted-secret-data";

        var result = _sut.Add(userId, encryptedSecret);

        result.Should().Be(encryptedSecret);
    }

    [Fact]
    public void Add_ShouldStoreMultipleSecretsForSameUser()
    {
        var userId = "user123";
        var secrets = new[] { "encrypted1", "encrypted2", "encrypted3" };

        foreach (var secret in secrets)
        {
            _sut.Add(userId, secret);
        }

        var storedSecrets = _sut.GetByUserId(userId);
        storedSecrets.Should().BeEquivalentTo(secrets);
    }

    [Fact]
    public void Add_ShouldStoreSecretsSeparatelyForDifferentUsers()
    {
        var user1 = "user123";
        var user2 = "user456";
        var secret1 = "encrypted-secret-1";
        var secret2 = "encrypted-secret-2";

        _sut.Add(user1, secret1);
        _sut.Add(user2, secret2);

        var user1Secrets = _sut.GetByUserId(user1);
        var user2Secrets = _sut.GetByUserId(user2);

        user1Secrets.Should().BeEquivalentTo(new[] { secret1 });
        user2Secrets.Should().BeEquivalentTo(new[] { secret2 });
    }

    [Fact]
    public void GetByUserId_WithNoSecretsStored_ShouldReturnEmptyCollection()
    {
        var userId = "nonexistent";

        var result = _sut.GetByUserId(userId);

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetByUserId_ShouldReturnAllSecretsForUser()
    {
        var userId = "user123";
        var secrets = new[] { "encrypted1", "encrypted2", "encrypted3" };

        foreach (var secret in secrets)
        {
            _sut.Add(userId, secret);
        }

        var result = _sut.GetByUserId(userId);

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
            tasks.Add(Task.Run(() => _sut.Add(userId, secret)));
        }

        await Task.WhenAll(tasks);

        var storedSecrets = _sut.GetByUserId(userId);
        storedSecrets.Should().HaveCount(100);
    }

    [Fact]
    public void Add_WithEmptyUserId_ShouldStoreSuccessfully()
    {
        var userId = "";
        var encryptedSecret = "encrypted-data";

        var result = _sut.Add(userId, encryptedSecret);

        result.Should().Be(encryptedSecret);
        var storedSecrets = _sut.GetByUserId(userId);
        storedSecrets.Should().ContainSingle().Which.Should().Be(encryptedSecret);
    }

    [Fact]
    public void Add_WithEmptyEncryptedSecret_ShouldStoreSuccessfully()
    {
        var userId = "user123";
        var encryptedSecret = "";

        var result = _sut.Add(userId, encryptedSecret);

        result.Should().Be(encryptedSecret);
        var storedSecrets = _sut.GetByUserId(userId);
        storedSecrets.Should().ContainSingle().Which.Should().Be(encryptedSecret);
    }

    [Fact]
    public void Add_ShouldMaintainOrderOfSecrets()
    {
        var userId = "user123";
        var secrets = new[] { "first", "second", "third", "fourth", "fifth" };

        foreach (var secret in secrets)
        {
            _sut.Add(userId, secret);
        }

        var storedSecrets = _sut.GetByUserId(userId);
        
        storedSecrets.Should().HaveCount(5);
        storedSecrets.Should().BeEquivalentTo(secrets);
    }
}