using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KestrelApi.IntegrationTests.Infrastructure;

public class TestAuthHandler : AuthenticationHandler<TestAuthHandlerOptions>
{
    public const string AuthenticationScheme = "Test";
    public const string AuthorizationHeaderName = "Authorization";
    public const string TestAuthHeaderValue = "Test";

    public TestAuthHandler(IOptionsMonitor<TestAuthHandlerOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check if Authorization header exists
        if (!Request.Headers.TryGetValue(AuthorizationHeaderName, out var authHeaderValue))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var authHeader = authHeaderValue.ToString();
        
        // Check if it's our test auth header
        if (!authHeader.StartsWith(TestAuthHeaderValue, StringComparison.Ordinal))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        // Create test claims
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Options.UserId ?? "test-user-123"),
            new Claim(ClaimTypes.Name, Options.UserName ?? "Test User"),
            new Claim(ClaimTypes.Email, Options.Email ?? "test@example.com")
        };

        // Add any additional claims from options
        if (Options.AdditionalClaims != null)
        {
            claims = claims.Concat(Options.AdditionalClaims).ToArray();
        }

        var identity = new ClaimsIdentity(claims, AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public class TestAuthHandlerOptions : AuthenticationSchemeOptions
{
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public IEnumerable<Claim>? AdditionalClaims { get; set; }
}