# KestrelApi Integration Tests

This project contains integration tests for the KestrelApi using xUnit and ASP.NET Core's WebApplicationFactory.

## Test Structure

- **Infrastructure/**
  - `KestrelApiFactory.cs` - Custom WebApplicationFactory for test setup
  - `IntegrationTestBase.cs` - Base class for all integration tests
  
- **Endpoints/**
  - Tests for various API endpoints

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
- ✅ Authentication and authorization flows
- ✅ Correct content-type headers
- ✅ 404 responses for non-existent endpoints
- ✅ User data isolation
- ✅ Permission-based access control

## Dependencies

- xUnit - Test framework
- Microsoft.AspNetCore.Mvc.Testing - Integration testing support
- FluentAssertions - Assertion library