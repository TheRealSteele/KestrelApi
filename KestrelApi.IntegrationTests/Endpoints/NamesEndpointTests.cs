using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using KestrelApi.IntegrationTests.Infrastructure;
using Xunit;

namespace KestrelApi.IntegrationTests.Endpoints;

[Collection("Integration")]
public class NamesEndpointTests : IntegrationTestBase
{
    public NamesEndpointTests(KestrelApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Post_Names_Should_Accept_Name_And_Return_Created()
    {
        // Arrange
        var endpoint = "/names";
        var request = new { name = "John Doe" };

        // Act
        var response = await AuthenticatedClient.PostAsJsonAsync(endpoint, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Get_Names_Should_Return_Previously_Stored_Names()
    {
        // Arrange
        var postEndpoint = "/names";
        var getEndpoint = "/names";
        var request = new { name = "Jane Smith" };
        
        // Act - First post a name
        await AuthenticatedClient.PostAsJsonAsync(postEndpoint, request);
        
        // Act - Then get all names
        var response = await AuthenticatedClient.GetAsync(getEndpoint);
        var names = await response.Content.ReadFromJsonAsync<string[]>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        names.Should().Contain("Jane Smith");
    }

    [Fact]
    public async Task Post_Names_Should_Store_Multiple_Names()
    {
        // Arrange
        var postEndpoint = "/names";
        var getEndpoint = "/names";
        var names = new[] { "Alice", "Bob", "Charlie" };
        
        // Act - Post multiple names concurrently
        var postTasks = names.Select(name => 
            AuthenticatedClient.PostAsJsonAsync(postEndpoint, new { name })
        ).ToArray();
        
        await Task.WhenAll(postTasks);
        
        // Act - Get all names
        var response = await AuthenticatedClient.GetAsync(getEndpoint);
        var storedNames = await response.Content.ReadFromJsonAsync<string[]>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        storedNames.Should().Contain(names);
        storedNames.Should().HaveCountGreaterOrEqualTo(3);
    }

    [Fact]
    public async Task Post_Names_With_Null_Name_Should_Return_BadRequest()
    {
        // Arrange
        var endpoint = "/names";
        var request = new { name = (string?)null };

        // Act
        var response = await AuthenticatedClient.PostAsJsonAsync(endpoint, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_Names_Without_Auth_Should_Return_Unauthorized()
    {
        // Arrange
        var endpoint = "/names";
        var request = new { name = "Unauthorized User" };

        // Act
        var response = await Client.PostAsJsonAsync(endpoint, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_Names_Without_Auth_Should_Return_Unauthorized()
    {
        // Arrange
        var endpoint = "/names";

        // Act
        var response = await Client.GetAsync(endpoint);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_Hello_Without_Auth_Should_Return_Ok()
    {
        // Arrange
        var endpoint = "/hello";

        // Act
        var response = await Client.GetAsync(endpoint);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Be("Hello World!");
    }
}