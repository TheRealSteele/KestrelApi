# Authentication and Authorization Testing in KestrelApi

This document explains the comprehensive approach to testing authentication and authorization in the KestrelApi project, demonstrating how to test different security requirements without external dependencies.

## Overview

The KestrelApi project uses two different authentication strategies, each suited to different security requirements:

1. **Simple Test Authentication** - For basic authenticated endpoints (e.g., `/names`)
2. **OAuth2/JWT Authentication** - For endpoints requiring permission-based authorization (e.g., `/secrets`)

## Test Infrastructure Components

### 1. OAuth2StubServer

The `OAuth2StubServer` class creates a mock OAuth2/OpenID Connect provider using WireMock:

```csharp
// Creates a WireMock server that mimics Auth0
_server = WireMockServer.Start();
_rsa = RSA.Create(2048);
SecurityKey = new RsaSecurityKey(_rsa) { KeyId = _keyId };
```

**Key Features:**
- Generates RSA keys for JWT signing
- Exposes OIDC discovery endpoint (`/.well-known/openid-configuration`)
- Exposes JWKS endpoint (`/.well-known/jwks.json`) for public key retrieval
- Provides signing credentials for generating valid JWTs

### 2. TestJwtGenerator

Generates various JWT tokens for testing different scenarios:

```csharp
// Generate token with write:secrets permission
var token = _jwtGenerator.GenerateToken(
    additionalClaims: new[] { new Claim("permissions", "write:secrets") });
```

**Token Types:**
- Valid tokens with configurable claims and permissions
- Expired tokens
- Tokens with wrong audience
- Tokens with wrong issuer
- Tokens with invalid signatures

### 3. Test Factories

#### KestrelApiFactory (Simple Authentication)
- Uses `TestAuthHandler` that accepts any request with `Authorization: Test` header
- No actual token validation
- Suitable for testing basic authentication requirements

#### KestrelApiFactoryWithAuth0 (Full OAuth2/JWT)
- Configures the test server to use the OAuth2 stub
- Implements real JWT validation
- Supports permission-based authorization testing

## Authentication Approaches by Endpoint

### /names Endpoint (Simple Authentication)

**Security Requirements:**
- Authentication required (`[Authorize]` attribute)
- No specific permissions needed
- Any authenticated user can read and write names

**Test Setup:**
```csharp
// In test
AuthenticatedClient = factory.CreateClient().WithTestAuth();
// This adds "Authorization: Test" header
```

### /secrets Endpoint (Permission-Based Authorization)

**Security Requirements:**
- GET: Authentication required (`[Authorize]`)
- POST: Authentication + `write:secrets` permission required (`[Authorize(Policy = "WriteSecrets")]`)

**Test Setup:**
```csharp
// In test
var token = _jwtGenerator.GenerateToken(
    additionalClaims: new[] { new Claim("permissions", "write:secrets") });
_client.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", token);
```

## Authorization Implementation Details

### PermissionAuthorizationHandler

The custom authorization handler checks for permission claims in JWT tokens:

```csharp
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var permissionClaims = context.User.FindAll("permissions").ToList();
        
        foreach (var claim in permissionClaims)
        {
            var permissions = claim.Value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim());
            
            if (permissions.Contains(requirement.Permission))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }
        }
        return Task.CompletedTask;
    }
}
```

**Features:**
- Supports single permission claims
- Handles comma-separated permissions
- Supports multiple permission claims (Auth0 pattern)

### Policy Configuration

```csharp
// In Program.cs
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("WriteSecrets", policy =>
        policy.Requirements.Add(new PermissionRequirement("write:secrets")));
});
```

## Test Organization Patterns

### Test Collections

Tests are organized into collections to prevent conflicts:

```csharp
[Collection("Integration")]          // For simple auth tests
[Collection("Auth0Integration")]     // For OAuth2/JWT tests
```

### Test Structure

All tests follow the Arrange-Act-Assert pattern:

```csharp
[Fact]
public async Task Post_WithoutWritePermission_ReturnsForbidden()
{
    // Arrange
    var token = _jwtGenerator.GenerateToken(); // No permissions
    _client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", token);
    
    // Act
    var response = await _client.PostAsJsonAsync("/secrets", 
        new { secret = "test" });
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
}
```

## Test Coverage

The test suite covers:

1. **Authentication Scenarios:**
   - Valid authentication
   - Missing authentication (401 Unauthorized)
   - Invalid tokens (expired, wrong issuer/audience, bad signature)

2. **Authorization Scenarios:**
   - Valid permissions
   - Missing permissions (403 Forbidden)
   - Multiple permission formats

3. **User Isolation:**
   - Different users see only their own data
   - Concurrent operations by different users

## Key Benefits of This Approach

1. **No External Dependencies:** Tests run completely offline without requiring Auth0 or other external services

2. **Realistic Testing:** The OAuth2 stub provides realistic JWT validation that matches production behavior

3. **Progressive Complexity:** Start with simple authentication and add sophisticated authorization as needed

4. **Comprehensive Security Testing:** Covers both positive and negative security scenarios

5. **Fast and Reliable:** Tests run quickly and consistently without network dependencies

## Usage Guidelines

### When to Use Simple Authentication (TestAuthHandler)
- Testing endpoints that only require authentication
- Rapid prototyping and development
- When permission-based authorization isn't needed

### When to Use OAuth2/JWT Authentication (OAuth2StubServer)
- Testing permission-based authorization
- Simulating production-like OAuth2 flows
- Testing various token validation scenarios
- When you need realistic Auth0-like behavior

## Example Test Scenarios

### Testing Authentication Only
```csharp
[Fact]
public async Task Get_Names_Without_Auth_Should_Return_Unauthorized()
{
    // No authorization header
    var response = await UnauthenticatedClient.GetAsync("/names");
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
}
```

### Testing Permission-Based Authorization
```csharp
[Fact]
public async Task Post_Secrets_Without_Permission_Should_Return_Forbidden()
{
    // Token without write:secrets permission
    var token = _jwtGenerator.GenerateToken();
    _client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", token);
    
    var response = await _client.PostAsJsonAsync("/secrets", 
        new { secret = "test-secret" });
    
    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
}
```

### Testing Multiple Users
```csharp
[Fact]
public async Task Different_Users_Should_Have_Isolated_Data()
{
    // Create tokens for different users
    var user1Token = _jwtGenerator.GenerateToken(userId: "user1", 
        additionalClaims: new[] { new Claim("permissions", "write:secrets") });
    var user2Token = _jwtGenerator.GenerateToken(userId: "user2", 
        additionalClaims: new[] { new Claim("permissions", "write:secrets") });
    
    // Each user adds their own secret
    // Verify each user only sees their own data
}
```

## Conclusion

This authentication testing approach provides a robust foundation for testing security requirements at various levels of complexity. It demonstrates best practices for testing authentication and authorization without external dependencies while maintaining realistic behavior that matches production systems.