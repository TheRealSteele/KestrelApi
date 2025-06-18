# KestrelApi Integration Tests

This project contains integration tests for the KestrelApi using xUnit and ASP.NET Core's WebApplicationFactory.

## Test Structure

- **Infrastructure/**
  - `KestrelApiFactory.cs` - Custom WebApplicationFactory for test setup
  - `IntegrationTestBase.cs` - Base class for all integration tests
  
- **Endpoints/**
  - `HelloEndpointTests.cs` - Tests for the /hello endpoint

## Running Tests

From the solution root directory:

```bash
# Run all tests
dotnet test

# Run only integration tests
dotnet test KestrelApi.IntegrationTests/KestrelApi.IntegrationTests.csproj

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"
```

## Test Coverage

The integration tests verify:
- ✅ HTTP GET /hello returns "Hello World!" with 200 OK
- ✅ Correct content-type headers
- ✅ 404 responses for non-existent endpoints
- ✅ 405 Method Not Allowed for non-GET methods on /hello

## Dependencies

- xUnit - Test framework
- Microsoft.AspNetCore.Mvc.Testing - Integration testing support
- FluentAssertions - Assertion library