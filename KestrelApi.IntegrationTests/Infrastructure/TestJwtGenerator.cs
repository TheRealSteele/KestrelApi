using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace KestrelApi.IntegrationTests.Infrastructure;

public class TestJwtGenerator
{
    private readonly OAuth2StubServer _stubServer;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    
    public TestJwtGenerator(OAuth2StubServer stubServer)
    {
        _stubServer = stubServer;
        _tokenHandler = new JwtSecurityTokenHandler();
    }
    
    public string GenerateToken(
        string userId = "test-user-123",
        string userName = "Test User",
        string email = "test@example.com",
        IEnumerable<Claim>? additionalClaims = null,
        TimeSpan? expiration = null)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, userName),
            new Claim(ClaimTypes.Email, email),
            new Claim("sub", userId),
            new Claim("email", email),
            new Claim("name", userName)
        };
        
        if (additionalClaims != null)
        {
            claims.AddRange(additionalClaims);
        }
        
        var now = DateTime.UtcNow;
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            NotBefore = now,
            Expires = now.Add(expiration ?? TimeSpan.FromHours(1)),
            IssuedAt = now,
            Issuer = _stubServer.Issuer,
            Audience = _stubServer.Audience,
            SigningCredentials = _stubServer.SigningCredentials
        };
        
        var token = _tokenHandler.CreateToken(tokenDescriptor);
        return _tokenHandler.WriteToken(token);
    }
    
    public string GenerateExpiredToken(
        string userId = "test-user-123",
        string userName = "Test User",
        string email = "test@example.com")
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, userName),
            new Claim(ClaimTypes.Email, email),
            new Claim("sub", userId),
            new Claim("email", email),
            new Claim("name", userName)
        };
        
        var now = DateTime.UtcNow;
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            NotBefore = now.AddHours(-2), // Set NotBefore to 2 hours ago
            Expires = now.AddHours(-1),    // Set Expires to 1 hour ago
            IssuedAt = now.AddHours(-2),   // Set IssuedAt to 2 hours ago
            Issuer = _stubServer.Issuer,
            Audience = _stubServer.Audience,
            SigningCredentials = _stubServer.SigningCredentials
        };
        
        var token = _tokenHandler.CreateToken(tokenDescriptor);
        return _tokenHandler.WriteToken(token);
    }
    
    public string GenerateTokenWithWrongAudience(
        string userId = "test-user-123",
        string userName = "Test User",
        string email = "test@example.com")
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, userName),
            new Claim(ClaimTypes.Email, email),
            new Claim("sub", userId),
            new Claim("email", email),
            new Claim("name", userName)
        };
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.Add(TimeSpan.FromHours(1)),
            Issuer = _stubServer.Issuer,
            Audience = "https://wrong-audience",
            SigningCredentials = _stubServer.SigningCredentials
        };
        
        var token = _tokenHandler.CreateToken(tokenDescriptor);
        return _tokenHandler.WriteToken(token);
    }
    
    public string GenerateTokenWithWrongIssuer(
        string userId = "test-user-123",
        string userName = "Test User",
        string email = "test@example.com")
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, userName),
            new Claim(ClaimTypes.Email, email),
            new Claim("sub", userId),
            new Claim("email", email),
            new Claim("name", userName)
        };
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.Add(TimeSpan.FromHours(1)),
            Issuer = "https://wrong-issuer.com/",
            Audience = _stubServer.Audience,
            SigningCredentials = _stubServer.SigningCredentials
        };
        
        var token = _tokenHandler.CreateToken(tokenDescriptor);
        return _tokenHandler.WriteToken(token);
    }
    
    public string GenerateTokenWithInvalidSignature()
    {
        // Create a token with a different RSA key
        using var rsa = RSA.Create(2048);
        var invalidKey = new RsaSecurityKey(rsa);
        var invalidCredentials = new SigningCredentials(invalidKey, SecurityAlgorithms.RsaSha256);
        
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-123"),
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.Email, "test@example.com"),
            new Claim("sub", "test-user-123"),
            new Claim("email", "test@example.com"),
            new Claim("name", "Test User")
        };
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.Add(TimeSpan.FromHours(1)),
            Issuer = _stubServer.Issuer,
            Audience = _stubServer.Audience,
            SigningCredentials = invalidCredentials
        };
        
        var token = _tokenHandler.CreateToken(tokenDescriptor);
        return _tokenHandler.WriteToken(token);
    }
}