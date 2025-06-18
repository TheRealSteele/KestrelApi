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
    public void Add_ShouldStoreNameAndReturnIt()
    {
        var userId = "user123";
        var name = "John Doe";

        var result = _sut.Add(userId, name);

        result.Should().Be(name);
    }

    [Fact]
    public void Add_ShouldStoreMultipleNamesForSameUser()
    {
        var userId = "user123";
        var names = new[] { "John Doe", "Jane Smith", "Bob Johnson" };

        foreach (var name in names)
        {
            _sut.Add(userId, name);
        }

        var storedNames = _sut.GetByUserId(userId);
        storedNames.Should().BeEquivalentTo(names);
    }

    [Fact]
    public void Add_ShouldStoreNamesSeparatelyForDifferentUsers()
    {
        var user1 = "user123";
        var user2 = "user456";
        var name1 = "John Doe";
        var name2 = "Jane Smith";

        _sut.Add(user1, name1);
        _sut.Add(user2, name2);

        var user1Names = _sut.GetByUserId(user1);
        var user2Names = _sut.GetByUserId(user2);

        user1Names.Should().BeEquivalentTo(new[] { name1 });
        user2Names.Should().BeEquivalentTo(new[] { name2 });
    }

    [Fact]
    public void GetByUserId_WithNoNamesStored_ShouldReturnEmptyCollection()
    {
        var userId = "nonexistent";

        var result = _sut.GetByUserId(userId);

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetByUserId_ShouldReturnAllNamesForUser()
    {
        var userId = "user123";
        var names = new[] { "Name1", "Name2", "Name3" };

        foreach (var name in names)
        {
            _sut.Add(userId, name);
        }

        var result = _sut.GetByUserId(userId);

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
            tasks.Add(Task.Run(() => _sut.Add(userId, name)));
        }

        await Task.WhenAll(tasks);

        var storedNames = _sut.GetByUserId(userId);
        storedNames.Should().HaveCount(100);
    }

    [Fact]
    public void Add_WithEmptyUserId_ShouldStoreSuccessfully()
    {
        var userId = "";
        var name = "John Doe";

        var result = _sut.Add(userId, name);

        result.Should().Be(name);
        var storedNames = _sut.GetByUserId(userId);
        storedNames.Should().ContainSingle().Which.Should().Be(name);
    }

    [Fact]
    public void Add_WithEmptyName_ShouldStoreSuccessfully()
    {
        var userId = "user123";
        var name = "";

        var result = _sut.Add(userId, name);

        result.Should().Be(name);
        var storedNames = _sut.GetByUserId(userId);
        storedNames.Should().ContainSingle().Which.Should().Be(name);
    }
}