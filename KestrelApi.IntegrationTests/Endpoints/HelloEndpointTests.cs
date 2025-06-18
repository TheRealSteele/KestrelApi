using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using KestrelApi.IntegrationTests.Infrastructure;
using Xunit;

namespace KestrelApi.IntegrationTests.Endpoints;

[Collection("Integration")]
public class HelloEndpointTests : IntegrationTestBase
{
    public HelloEndpointTests(KestrelApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Get_Hello_Returns_Ok_With_HelloWorld_Message()
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

    [Fact]
    public async Task Get_Hello_Returns_Correct_ContentType()
    {
        // Arrange
        var endpoint = "/hello";

        // Act
        var response = await Client.GetAsync(endpoint);

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/plain");
    }

    [Fact]
    public async Task Get_NonExistentEndpoint_Returns_NotFound()
    {
        // Arrange
        var endpoint = "/nonexistent";

        // Act
        var response = await Client.GetAsync(endpoint);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    [InlineData("PATCH")]
    public async Task Hello_Endpoint_With_NonGet_Methods_Returns_MethodNotAllowed(string method)
    {
        // Arrange
        var endpoint = "/hello";
        using var request = new HttpRequestMessage(new HttpMethod(method), endpoint);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    }
}