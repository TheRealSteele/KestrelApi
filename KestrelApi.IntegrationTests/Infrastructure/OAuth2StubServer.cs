using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace KestrelApi.IntegrationTests.Infrastructure;

public class OAuth2StubServer : IDisposable
{
    private readonly WireMockServer _server;
    private readonly RSA _rsa;
    private readonly string _keyId;
    
    public Uri BaseUrl => new Uri(_server.Url!);
    public string Issuer => BaseUrl.ToString();
    public string Audience { get; }
    public RsaSecurityKey SecurityKey { get; }
    public SigningCredentials SigningCredentials { get; }
    
    public OAuth2StubServer(string audience = "https://test-api")
    {
        _server = WireMockServer.Start();
        _rsa = RSA.Create(2048);
        _keyId = Guid.NewGuid().ToString();
        Audience = audience;
        
        SecurityKey = new RsaSecurityKey(_rsa) { KeyId = _keyId };
        SigningCredentials = new SigningCredentials(SecurityKey, SecurityAlgorithms.RsaSha256)
        {
            CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false }
        };
        
        SetupEndpoints();
    }
    
    private void SetupEndpoints()
    {
        // OIDC Discovery endpoint
        _server
            .Given(Request.Create()
                .WithPath("/.well-known/openid-configuration")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new
                {
                    issuer = Issuer,
                    jwks_uri = $"{BaseUrl}/.well-known/jwks.json",
                    authorization_endpoint = $"{BaseUrl}/authorize",
                    token_endpoint = $"{BaseUrl}/oauth/token",
                    response_types_supported = new[] { "code", "token", "id_token" },
                    subject_types_supported = new[] { "public" },
                    id_token_signing_alg_values_supported = new[] { "RS256" }
                }));
        
        // JWKS endpoint
        _server
            .Given(Request.Create()
                .WithPath("/.well-known/jwks.json")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new
                {
                    keys = new[]
                    {
                        CreateJwk()
                    }
                }));
    }
    
    private object CreateJwk()
    {
        var parameters = _rsa.ExportParameters(false);
        
        return new
        {
            kty = "RSA",
            use = "sig",
            kid = _keyId,
            alg = "RS256",
            n = Base64UrlEncoder.Encode(parameters.Modulus),
            e = Base64UrlEncoder.Encode(parameters.Exponent)
        };
    }
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _server?.Stop();
            _server?.Dispose();
            _rsa?.Dispose();
        }
    }
}