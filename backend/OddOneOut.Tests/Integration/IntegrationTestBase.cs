using Microsoft.Extensions.DependencyInjection;
using OddOneOut.Data;
using OddOneOut.Tests.TestFixtures;
using System.Net.Http.Json;

namespace OddOneOut.Tests.Integration;

/// <summary>
/// Base class for integration tests providing common setup and utilities.
/// </summary>
public abstract class IntegrationTestBase : IClassFixture<CustomWebApplicationFactory>, IDisposable
{
    protected readonly CustomWebApplicationFactory Factory;
    protected readonly HttpClient Client;

    protected IntegrationTestBase(CustomWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    /// <summary>
    /// Creates an authenticated HTTP client for the specified user.
    /// </summary>
    protected HttpClient CreateAuthenticatedClient(string userId)
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.AuthenticatedUserIdHeader, userId);
        return client;
    }

    /// <summary>
    /// Gets the database context for direct database operations.
    /// </summary>
    protected AppDbContext GetDbContext()
    {
        var scope = Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    /// <summary>
    /// Creates a test user in the database.
    /// </summary>
    protected User CreateTestUser(string userId, string userName = "TestUser")
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return TestDataSeeder.CreateTestUser(db, userId, userName);
    }

    /// <summary>
    /// Creates a game with card set for testing.
    /// </summary>
    protected (CardSet cardSet, Game game) CreateGameWithCardSet(User clueGiver, string clue = "fruit")
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        // Attach the user to the context
        db.Attach(clueGiver);
        
        return TestDataSeeder.CreateGameWithCardSet(db, clueGiver, clue);
    }

    public virtual void Dispose()
    {
        Client.Dispose();
    }
}

/// <summary>
/// HTTP client extensions for common test operations.
/// </summary>
public static class HttpClientTestExtensions
{
    public static async Task<T?> PostAsJsonAsync<T>(this HttpClient client, string requestUri, object? content = null)
    {
        var response = await client.PostAsJsonAsync(requestUri, content ?? new { });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>();
    }

    public static async Task<T?> GetFromJsonAsync<T>(this HttpClient client, string requestUri)
    {
        return await System.Net.Http.Json.HttpClientJsonExtensions.GetFromJsonAsync<T>(client, requestUri);
    }
}
