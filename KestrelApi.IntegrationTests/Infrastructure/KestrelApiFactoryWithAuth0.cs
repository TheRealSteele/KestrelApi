using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace KestrelApi.IntegrationTests.Infrastructure;

public class KestrelApiFactoryWithAuth0 : WebApplicationFactory<Program>
{
    private OAuth2StubServer? _stubServer;
    public OAuth2StubServer StubServer => _stubServer!;
    
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        
        // Create the stub server before configuration
        _stubServer = new OAuth2StubServer("https://test-api");
        
        // Configure app configuration BEFORE services are configured
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Clear existing sources to ensure our configuration takes precedence
            config.Sources.Clear();
            
            // Add in-memory configuration that overrides appsettings
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth0:Domain"] = _stubServer.Issuer,
                ["Auth0:Audience"] = _stubServer.Audience,
                ["Serilog:MinimumLevel:Default"] = "Warning",
                ["AllowedHosts"] = "*"
            });
        });
        
        builder.ConfigureTestServices(services =>
        {
            // Configure JWT Bearer options for test environment
            services.Configure<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>(
                Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme,
                options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.Authority = _stubServer.Issuer;
                    options.Audience = _stubServer.Audience;
                    options.BackchannelTimeout = TimeSpan.FromSeconds(30);
                    options.MetadataAddress = $"{_stubServer.Issuer}/.well-known/openid-configuration";
                    
                    // Skip metadata discovery for tests
                    options.Configuration = new Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration
                    {
                        Issuer = _stubServer.Issuer
                    };
                    options.Configuration.SigningKeys.Add(_stubServer.SecurityKey);
                    
                    // Ensure the token validation parameters are set correctly
                    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = _stubServer.Issuer,
                        ValidateAudience = true,
                        ValidAudience = _stubServer.Audience,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = _stubServer.SecurityKey,
                        ClockSkew = TimeSpan.Zero
                    };
                });
        });

        builder.UseEnvironment("Test");
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        
        builder.ConfigureHostConfiguration(config =>
        {
            // Ensure our test configuration takes precedence
        });

        return base.CreateHost(builder);
    }
    
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _stubServer?.Dispose();
        }
        base.Dispose(disposing);
    }
}