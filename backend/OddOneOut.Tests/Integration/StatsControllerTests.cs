using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OddOneOut.Data;
using OddOneOut.Tests.TestFixtures;
using System.Net;
using System.Net.Http.Json;

namespace OddOneOut.Tests.Integration;

/// <summary>
/// Integration tests for the StatsController API endpoints.
/// </summary>
public class StatsControllerTests : IntegrationTestBase
{
    public StatsControllerTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    #region GuessHistory Tests

    [Fact]
    public async Task GuessHistory_ReturnsUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        var client = Factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/Stats/GuessHistory");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GuessHistory_ReturnsEmptyList_WhenNoGuesses()
    {
        // Arrange
        var userId = $"test-user-{Guid.NewGuid()}";
        var user = CreateTestUser(userId);
        var client = CreateAuthenticatedClient(userId);

        // Act
        var response = await client.GetAsync("/api/Stats/GuessHistory");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<GuessHistoryItem>>();
        result.Should().NotBeNull();
        result!.Data.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GuessHistory_ReturnsPaginatedResults()
    {
        // Arrange
        var userId = $"test-user-{Guid.NewGuid()}";
        var user = CreateTestUser(userId);
        
        // Create some guesses for the user
        var clueGiver = CreateTestUser($"clue-giver-{Guid.NewGuid()}", "ClueGiver");
        
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var dbUser = db.Users.Find(userId);
            var dbClueGiver = db.Users.Find(clueGiver.Id);
            
            var (cardSet, game) = TestDataSeeder.CreateGameWithCardSet(db, dbClueGiver!);
            
            // Create multiple guesses
            var wordCards = game.CardSet!.WordCards;
            for (int i = 0; i < 5; i++)
            {
                TestDataSeeder.CreateGuess(db, game, dbUser!, wordCards[i % wordCards.Count], true);
            }
        }

        var client = CreateAuthenticatedClient(userId);

        // Act
        var response = await client.GetAsync("/api/Stats/GuessHistory?page=1&pageSize=3");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<GuessHistoryItem>>();
        result.Should().NotBeNull();
        result!.Data.Should().HaveCount(3);
        result.TotalCount.Should().Be(5);
        result.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task GuessHistory_RespectsPageParameter()
    {
        // Arrange
        var userId = $"test-user-{Guid.NewGuid()}";
        var user = CreateTestUser(userId);
        
        var clueGiver = CreateTestUser($"clue-giver-{Guid.NewGuid()}", "ClueGiver");
        
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var dbUser = db.Users.Find(userId);
            var dbClueGiver = db.Users.Find(clueGiver.Id);
            
            var (cardSet, game) = TestDataSeeder.CreateGameWithCardSet(db, dbClueGiver!);
            
            var wordCards = game.CardSet!.WordCards;
            for (int i = 0; i < 5; i++)
            {
                TestDataSeeder.CreateGuess(db, game, dbUser!, wordCards[i % wordCards.Count], true);
            }
        }

        var client = CreateAuthenticatedClient(userId);

        // Act
        var response = await client.GetAsync("/api/Stats/GuessHistory?page=2&pageSize=3");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<GuessHistoryItem>>();
        result.Should().NotBeNull();
        result!.Data.Should().HaveCount(2); // Remaining 2 items on page 2
        result.Page.Should().Be(2);
    }

    #endregion

    #region ClueHistory Tests

    [Fact]
    public async Task ClueHistory_ReturnsUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        var client = Factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/Stats/ClueHistory");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ClueHistory_ReturnsEmptyList_WhenNoClues()
    {
        // Arrange
        var userId = $"test-user-{Guid.NewGuid()}";
        var user = CreateTestUser(userId);
        var client = CreateAuthenticatedClient(userId);

        // Act
        var response = await client.GetAsync("/api/Stats/ClueHistory");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<ClueHistoryResponse>();
        result.Should().NotBeNull();
        result!.Data.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task ClueHistory_ReturnsPaginatedResults()
    {
        // Arrange
        var userId = $"test-user-{Guid.NewGuid()}";
        var user = CreateTestUser(userId);
        
        // Create some games for the user as a clue giver
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var dbUser = db.Users.Find(userId);
            
            for (int i = 0; i < 5; i++)
            {
                TestDataSeeder.CreateGameWithCardSet(db, dbUser!, $"clue{i}");
            }
        }

        var client = CreateAuthenticatedClient(userId);

        // Act
        var response = await client.GetAsync("/api/Stats/ClueHistory?page=1&pageSize=3");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<ClueHistoryResponse>();
        result.Should().NotBeNull();
        result!.Data.Should().HaveCount(3);
        result.TotalCount.Should().Be(5);
    }

    #endregion

    #region Leaderboard Tests

    [Fact]
    public async Task GuessLeaderboard_ReturnsTop10Users()
    {
        // Arrange
        // Create multiple users with different ratings
        for (int i = 0; i < 15; i++)
        {
            using var scope = Factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = TestDataSeeder.CreateTestUser(db, $"leaderboard-user-{i}", $"Player{i}");
            user.GuessRating = 1000 + (i * 100); // Varying ratings
            user.IsGuest = false; // Not guests
            db.SaveChanges();
        }

        // Act
        var response = await Client.GetAsync("/api/Stats/GuessLeaderboard");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<List<LeaderboardEntry>>();
        result.Should().NotBeNull();
        result!.Count.Should().BeLessThanOrEqualTo(10);
        
        // Should be sorted by rating descending
        for (int i = 0; i < result.Count - 1; i++)
        {
            result[i].GuessRating.Should().BeGreaterThanOrEqualTo(result[i + 1].GuessRating);
        }
    }

    [Fact]
    public async Task GuessLeaderboard_ExcludesGuestUsers()
    {
        // Arrange
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            // Create a guest user with high rating
            var guest = TestDataSeeder.CreateTestUser(db, $"guest-high-{Guid.NewGuid()}", "GuestPlayer");
            guest.GuessRating = 9999; // Very high rating
            guest.IsGuest = true;
            
            // Create a regular user with lower rating
            var regular = TestDataSeeder.CreateTestUser(db, $"regular-{Guid.NewGuid()}", "RegularPlayer");
            regular.GuessRating = 1500;
            regular.IsGuest = false;
            
            db.SaveChanges();
        }

        // Act
        var response = await Client.GetAsync("/api/Stats/GuessLeaderboard");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<List<LeaderboardEntry>>();
        result.Should().NotBeNull();
        
        // Guest user should not be in leaderboard
        result!.Should().NotContain(e => e.UserName == "GuestPlayer");
    }

    [Fact]
    public async Task ClueLeaderboard_ReturnsTop10Users()
    {
        // Arrange
        for (int i = 0; i < 15; i++)
        {
            using var scope = Factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = TestDataSeeder.CreateTestUser(db, $"clue-leaderboard-user-{i}", $"CluePlayer{i}");
            user.CachedClueRating = 1000 + (i * 100);
            user.IsGuest = false;
            db.SaveChanges();
        }

        // Act
        var response = await Client.GetAsync("/api/Stats/ClueLeaderboard");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<List<ClueLeaderboardEntry>>();
        result.Should().NotBeNull();
        result!.Count.Should().BeLessThanOrEqualTo(10);
        
        // Should be sorted by rating descending
        for (int i = 0; i < result.Count - 1; i++)
        {
            result[i].ClueRating.Should().BeGreaterThanOrEqualTo(result[i + 1].ClueRating);
        }
    }

    #endregion
}

// Response DTOs
public class PaginatedResponse<T>
{
    public List<T> Data { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

public class GuessHistoryItem
{
    public Guid Game { get; set; }
    public Guid CardSetId { get; set; }
    public string? SelectedCard { get; set; }
    public string? OddOneOut { get; set; }
    public string? Clue { get; set; }
    public DateTime GuessedAt { get; set; }
    public bool GuessIsInSet { get; set; }
    public int RatingChange { get; set; }
}

public class ClueHistoryResponse
{
    public List<ClueHistoryItem> Data { get; set; } = new();
    public float ClueRating { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

public class ClueHistoryItem
{
    public Guid Game { get; set; }
    public Guid CardSetId { get; set; }
    public string? Clue { get; set; }
    public string? OddOneOut { get; set; }
    public DateTime CreatedAt { get; set; }
    public float GameScore { get; set; }
}

public class LeaderboardEntry
{
    public string Id { get; set; } = "";
    public string? UserName { get; set; }
    public int GuessRating { get; set; }
    public int Rank { get; set; }
}

public class ClueLeaderboardEntry
{
    public string Id { get; set; } = "";
    public string? UserName { get; set; }
    public float ClueRating { get; set; }
    public int Rank { get; set; }
}
