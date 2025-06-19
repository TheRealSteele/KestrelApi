# Authentication and Authorization Testing in KestrelApi

This document explains the comprehensive approach to testing authentication and authorization in the KestrelApi project, demonstrating how to test different security requirements without external dependencies.

## Overview

The KestrelApi project implements a robust permission-based authorization system using JWT tokens and OAuth2 patterns. The testing infrastructure supports two authentication strategies:

1. **Simple Test Authentication** - For basic authenticated endpoints (e.g., `/names`)
2. **OAuth2/JWT Authentication with Permissions** - For endpoints requiring fine-grained permission-based authorization (e.g., `/secrets`)

## Security Model

### Permission-Based Authorization
The API uses a **permission-based authorization model** (not role-based) that aligns with Auth0 and OAuth2 best practices:

- **Permissions**: Granular capabilities like `write:secrets`, `read:secrets`, `admin:users`
- **Claims**: Permissions are embedded in JWT tokens as `permissions` claims
- **Policies**: ASP.NET Core authorization policies map to specific permission requirements

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

Generates various JWT tokens for testing different permission scenarios:

```csharp
// Generate token with write:secrets permission
var token = _jwtGenerator.GenerateToken(
    userId: "user123",
    additionalClaims: new[] { new Claim("permissions", "write:secrets") });

// Generate token with multiple permissions (separate claims)
var token = _jwtGenerator.GenerateToken(
    userId: "user123", 
    additionalClaims: new[] 
    { 
        new Claim("permissions", "read:secrets"),
        new Claim("permissions", "write:secrets"),
        new Claim("permissions", "admin:users")
    });

// Generate token with comma-separated permissions (single claim)
var token = _jwtGenerator.GenerateToken(
    userId: "user123",
    additionalClaims: new[] { new Claim("permissions", "read:secrets,write:secrets,admin:users") });
```

**Token Types:**
- Valid tokens with configurable claims and permissions
- Tokens with multiple permission claims (Auth0 array pattern)
- Tokens with comma-separated permissions (Auth0 string pattern)
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
- GET: Authentication required (`[Authorize]`) - Any authenticated user can read their own secrets
- POST: Authentication + `write:secrets` permission required (`[Authorize(Policy = "WriteSecrets")]`)

**Design Decision:** The GET endpoint intentionally does NOT require `write:secrets` permission to maintain backward compatibility and allow users to read their existing secrets even if their permissions are later restricted.

**Test Setup:**
```csharp
// Test POST with proper permission
var token = _jwtGenerator.GenerateToken(
    userId: "user123",
    additionalClaims: new[] { new Claim("permissions", "write:secrets") });
_client.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", token);

// Test with multiple permissions (Auth0 array pattern)
var token = _jwtGenerator.GenerateToken(
    userId: "user123",
    additionalClaims: new[] 
    { 
        new Claim("permissions", "read:secrets"),
        new Claim("permissions", "write:secrets") 
    });

// Test with comma-separated permissions (Auth0 string pattern)  
var token = _jwtGenerator.GenerateToken(
    userId: "user123",
    additionalClaims: new[] { new Claim("permissions", "read:secrets,write:secrets") });
```

## Authorization Implementation Details

### PermissionAuthorizationHandler

The custom authorization handler implements flexible permission checking that supports Auth0's various permission claim patterns:

```csharp
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);
        
        // Check for permissions claims (Auth0 can send as array of claims)
        var permissionClaims = context.User.FindAll("permissions").ToList();
        
        if (permissionClaims.Count > 0)
        {
            // Check each permission claim
            foreach (var claim in permissionClaims)
            {
                // Single claim might contain comma-separated values
                var permissions = claim.Value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim());
                
                if (permissions.Contains(requirement.Permission))
                {
                    context.Succeed(requirement);
                    return Task.CompletedTask;
                }
            }
        }

        return Task.CompletedTask;
    }
}
```

**Key Features:**
- **Multiple Claims Support**: Handles Auth0's array-based permission claims (multiple `permissions` claims)
- **Comma-Separated Values**: Parses comma-separated permissions within a single claim
- **Whitespace Tolerance**: Trims whitespace from permission values
- **Fail-Safe Design**: Returns without explicitly failing, allowing other handlers to run
- **Null Safety**: Includes argument validation

**Auth0 Compatibility:**
This implementation supports both Auth0 permission claim formats:
1. **Array Format**: Multiple separate `permissions` claims
2. **String Format**: Single `permissions` claim with comma-separated values

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

The test suite provides comprehensive coverage across multiple dimensions:

### 1. Authentication Scenarios
- **Valid authentication**: Properly signed JWT tokens with correct issuer/audience
- **Missing authentication**: No Authorization header (401 Unauthorized)
- **Invalid tokens**: 
  - Expired tokens (401 Unauthorized)
  - Wrong issuer (401 Unauthorized)
  - Wrong audience (401 Unauthorized)  
  - Invalid signature (401 Unauthorized)

### 2. Permission-Based Authorization Scenarios
- **Valid permissions**: 
  - Single permission in single claim
  - Multiple permissions in multiple claims (Auth0 array pattern)
  - Comma-separated permissions in single claim (Auth0 string pattern)
- **Missing permissions**: No `write:secrets` permission (403 Forbidden)
- **Empty permissions**: Empty permissions claim (403 Forbidden)
- **Mixed permission scenarios**: User has some permissions but not the required one

### 3. Endpoint-Specific Authorization
- **GET /secrets**: Requires authentication only (backward compatibility)
- **POST /secrets**: Requires authentication + `write:secrets` permission
- **GET /names**: Requires authentication only
- **POST /names**: Requires authentication only

### 4. User Isolation & Data Segregation
- **Different users see only their own data**: JWT `sub` claim determines data isolation
- **Concurrent operations by different users**: Parallel requests maintain isolation
- **Permission inheritance**: Users retain access to previously created data even if permissions change

### 5. Edge Cases & Error Handling
- **Whitespace in permissions**: Permissions with leading/trailing spaces
- **Case sensitivity**: Permission matching is case-sensitive
- **Malformed claims**: Invalid or corrupted permission claims
- **Multiple authorization attempts**: Proper handling of repeated authorization checks

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
    var token = _jwtGenerator.GenerateToken(
        userId: "user-no-perms",
        additionalClaims: new[] { new Claim("permissions", "read:secrets") });
    _client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", token);
    
    var response = await _client.PostAsJsonAsync("/secrets", 
        new { secret = "test-secret" });
    
    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
}
```

### Testing Multiple Permission Formats
```csharp
[Fact]
public async Task Post_Secrets_With_Comma_Separated_Permissions_Should_Succeed()
{
    // Auth0 string format: comma-separated permissions in single claim
    var token = _jwtGenerator.GenerateToken(
        userId: "user-comma-perms",
        additionalClaims: new[] { new Claim("permissions", "read:secrets,write:secrets,admin:users") });
    _client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", token);
    
    var response = await _client.PostAsJsonAsync("/secrets", 
        new { secret = "test-secret" });
    
    response.StatusCode.Should().Be(HttpStatusCode.Created);
}

[Fact]
public async Task Post_Secrets_With_Multiple_Permission_Claims_Should_Succeed()
{
    // Auth0 array format: multiple separate permission claims
    var token = _jwtGenerator.GenerateToken(
        userId: "user-array-perms",
        additionalClaims: new[] 
        { 
            new Claim("permissions", "read:secrets"),
            new Claim("permissions", "write:secrets"),
            new Claim("permissions", "admin:users")
        });
    _client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", token);
    
    var response = await _client.PostAsJsonAsync("/secrets", 
        new { secret = "test-secret" });
    
    response.StatusCode.Should().Be(HttpStatusCode.Created);
}
```

### Testing User Isolation & Data Segregation
```csharp
[Fact]
public async Task Different_Users_Should_Have_Isolated_Data()
{
    // Create tokens for different users with same permissions
    var user1Token = _jwtGenerator.GenerateToken(
        userId: "user1", 
        additionalClaims: new[] { new Claim("permissions", "write:secrets") });
    var user2Token = _jwtGenerator.GenerateToken(
        userId: "user2", 
        additionalClaims: new[] { new Claim("permissions", "write:secrets") });
    
    // User 1 creates a secret
    _client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", user1Token);
    await _client.PostAsJsonAsync("/secrets", new { secret = "user1-secret" });
    
    // User 2 creates a different secret
    _client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", user2Token);
    await _client.PostAsJsonAsync("/secrets", new { secret = "user2-secret" });
    
    // Verify each user only sees their own data
    var user1Response = await _client.GetAsync("/secrets");
    var user1Secrets = await user1Response.Content.ReadFromJsonAsync<string[]>();
    user1Secrets.Should().Contain("user1-secret");
    user1Secrets.Should().NotContain("user2-secret");
}
```

### Testing Backward Compatibility
```csharp
[Fact]
public async Task Get_Secrets_Without_Write_Permission_Should_Still_Return_200()
{
    // User creates secret when they have write permission
    var writeToken = _jwtGenerator.GenerateToken(
        userId: "user123",
        additionalClaims: new[] { new Claim("permissions", "write:secrets") });
    _client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", writeToken);
    await _client.PostAsJsonAsync("/secrets", new { secret = "existing-secret" });
    
    // Later, user's permissions are restricted (no write permission)
    var readOnlyToken = _jwtGenerator.GenerateToken(
        userId: "user123",
        additionalClaims: new[] { new Claim("permissions", "read:profile") });
    _client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", readOnlyToken);
    
    // User should still be able to read their existing secrets
    var response = await _client.GetAsync("/secrets");
    response.StatusCode.Should().Be(HttpStatusCode.OK);
}
```

## Testing Philosophy & Best Practices

### Progressive Security Testing
The testing approach follows a **progressive complexity model**:

1. **Level 1 - Authentication Only**: Simple test auth for basic protected endpoints
2. **Level 2 - Permission-Based**: OAuth2/JWT with fine-grained permissions  
3. **Level 3 - Multi-Format Support**: Auth0 array and string permission patterns
4. **Level 4 - Real-World Scenarios**: User isolation, permission changes, edge cases

### Production Alignment
The test infrastructure closely mirrors production Auth0 behavior:

- **JWT Structure**: Same token structure and claims as Auth0
- **Permission Patterns**: Both array and string formats supported
- **Error Responses**: Identical 401/403 status codes and response format
- **User Isolation**: Same `sub` claim-based data segregation

### Test Maintainability
- **Shared Test Infrastructure**: OAuth2StubServer and TestJwtGenerator are reusable
- **Clear Test Organization**: Collections prevent cross-contamination
- **Descriptive Test Names**: Test intentions are clear from method names
- **Comprehensive Coverage**: Both positive and negative scenarios tested

### Security by Design
- **Fail-Safe Defaults**: Authorization fails closed (deny by default)
- **Defense in Depth**: Multiple layers of security testing
- **Edge Case Coverage**: Malformed tokens, empty permissions, etc.
- **Backward Compatibility**: Ensures existing functionality remains secure

## Conclusion

This authentication testing approach provides a robust foundation for testing security requirements at various levels of complexity. It demonstrates best practices for testing authentication and authorization without external dependencies while maintaining realistic behavior that matches production systems.