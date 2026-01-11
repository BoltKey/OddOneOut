using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OddOneOut.Tests.TestFixtures;

/// <summary>
/// Authentication handler for tests that bypasses real authentication.
/// Allows setting a specific user ID for each test request.
/// </summary>
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string TestUserId = "test-user-id";
    public const string TestUserName = "TestUser";
    public const string AuthenticatedUserIdHeader = "X-Test-User-Id";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check if authentication is requested via header
        if (!Request.Headers.ContainsKey(AuthenticatedUserIdHeader))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var userId = Request.Headers[AuthenticatedUserIdHeader].ToString();
        if (string.IsNullOrEmpty(userId))
        {
            userId = TestUserId;
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, TestUserName),
        };

        var identity = new ClaimsIdentity(claims, "TestScheme");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "TestScheme");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

/// <summary>
/// Extension methods for HttpClient to add test authentication headers.
/// </summary>
public static class TestAuthExtensions
{
    /// <summary>
    /// Sets the authenticated user for test requests.
    /// </summary>
    public static HttpClient WithTestAuth(this HttpClient client, string userId)
    {
        client.DefaultRequestHeaders.Add(TestAuthHandler.AuthenticatedUserIdHeader, userId);
        return client;
    }

    /// <summary>
    /// Sets the default test user for requests.
    /// </summary>
    public static HttpClient WithDefaultTestAuth(this HttpClient client)
    {
        return client.WithTestAuth(TestAuthHandler.TestUserId);
    }
}
