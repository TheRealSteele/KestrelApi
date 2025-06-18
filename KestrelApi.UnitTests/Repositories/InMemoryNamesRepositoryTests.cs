using FluentAssertions;
using KestrelApi.Names;

namespace KestrelApi.UnitTests.Repositories;

public class InMemoryNamesRepositoryTests
{
    private readonly InMemoryNamesRepository _sut;

    public InMemoryNamesRepositoryTests()
    {
        _sut = new InMemoryNamesRepository();
    }

    [Fact]
    public async Task AddAsync_ShouldStoreNameAndReturnIt()
    {
        var userId = "user123";
        var name = "John Doe";

        var result = await _sut.AddAsync(userId, name);

        result.Should().Be(name);
    }

    [Fact]
    public async Task AddAsync_ShouldStoreMultipleNamesForSameUser()
    {
        var userId = "user123";
        var names = new[] { "John Doe", "Jane Smith", "Bob Johnson" };

        foreach (var name in names)
        {
            await _sut.AddAsync(userId, name);
        }

        var storedNames = await _sut.GetByUserIdAsync(userId);
        storedNames.Should().BeEquivalentTo(names);
    }

    [Fact]
    public async Task AddAsync_ShouldStoreNamesSeparatelyForDifferentUsers()
    {
        var user1 = "user123";
        var user2 = "user456";
        var name1 = "John Doe";
        var name2 = "Jane Smith";

        await _sut.AddAsync(user1, name1);
        await _sut.AddAsync(user2, name2);

        var user1Names = await _sut.GetByUserIdAsync(user1);
        var user2Names = await _sut.GetByUserIdAsync(user2);

        user1Names.Should().BeEquivalentTo(new[] { name1 });
        user2Names.Should().BeEquivalentTo(new[] { name2 });
    }

    [Fact]
    public async Task GetByUserIdAsync_WithNoNamesStored_ShouldReturnEmptyCollection()
    {
        var userId = "nonexistent";

        var result = await _sut.GetByUserIdAsync(userId);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnAllNamesForUser()
    {
        var userId = "user123";
        var names = new[] { "Name1", "Name2", "Name3" };

        foreach (var name in names)
        {
            await _sut.AddAsync(userId, name);
        }

        var result = await _sut.GetByUserIdAsync(userId);

        result.Should().BeEquivalentTo(names);
    }

    [Fact]
    public async Task Repository_ShouldBeThreadSafe()
    {
        var userId = "user123";
        var tasks = new List<Task>();

        for (int i = 0; i < 100; i++)
        {
            var name = $"Name{i}";
            tasks.Add(Task.Run(async () => await _sut.AddAsync(userId, name)));
        }

        await Task.WhenAll(tasks);

        var storedNames = await _sut.GetByUserIdAsync(userId);
        storedNames.Should().HaveCount(100);
    }

    [Fact]
    public async Task AddAsync_WithEmptyUserId_ShouldStoreSuccessfully()
    {
        var userId = "";
        var name = "John Doe";

        var result = await _sut.AddAsync(userId, name);

        result.Should().Be(name);
        var storedNames = await _sut.GetByUserIdAsync(userId);
        storedNames.Should().ContainSingle().Which.Should().Be(name);
    }

    [Fact]
    public async Task AddAsync_WithEmptyName_ShouldStoreSuccessfully()
    {
        var userId = "user123";
        var name = "";

        var result = await _sut.AddAsync(userId, name);

        result.Should().Be(name);
        var storedNames = await _sut.GetByUserIdAsync(userId);
        storedNames.Should().ContainSingle().Which.Should().Be(name);
    }
}