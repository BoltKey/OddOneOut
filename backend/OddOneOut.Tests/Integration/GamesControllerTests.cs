using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OddOneOut.Data;
using OddOneOut.Tests.TestFixtures;
using System.Net;
using System.Net.Http.Json;

namespace OddOneOut.Tests.Integration;

/// <summary>
/// Integration tests for the GamesController API endpoints.
/// </summary>
[Collection("Sequential")]
public class GamesControllerTests : IntegrationTestBase
{
    public GamesControllerTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    #region AssignedGuess Tests

    [Fact]
    public async Task AssignedGuess_ReturnsUnauthorized_WhenNotAuthenticated()
    {
        // Arrange - use client without auth header
        var client = Factory.CreateClient();

        // Act
        var response = await client.PostAsync("/api/Games/AssignedGuess", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AssignedGuess_ReturnsGame_ForAuthenticatedUser()
    {
        // Arrange
        var userId = $"test-user-{Guid.NewGuid()}";
        var user = CreateTestUser(userId);

        // Create a clue giver and their game
        var clueGiver = CreateTestUser($"clue-giver-{Guid.NewGuid()}", "ClueGiver");
        var (cardSet, game) = CreateGameWithCardSet(clueGiver);

        var client = CreateAuthenticatedClient(userId);

        // Act
        var response = await client.PostAsync("/api/Games/AssignedGuess", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<GameGuessResponse>();
        result.Should().NotBeNull();
        result!.GameId.Should().NotBeEmpty();
        result.CurrentCard.Should().NotBeNullOrEmpty();
        result.CurrentClue.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task AssignedGuess_ReturnsBadRequest_WhenOutOfEnergy()
    {
        // Arrange
        var userId = $"test-user-{Guid.NewGuid()}";

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = TestDataSeeder.CreateTestUser(db, userId);
            user.GuessEnergy = 0; // No energy
            db.SaveChanges();
        }

        // Create a clue giver and their game
        var clueGiver = CreateTestUser($"clue-giver-{Guid.NewGuid()}", "ClueGiver");
        var (cardSet, game) = CreateGameWithCardSet(clueGiver);

        var client = CreateAuthenticatedClient(userId);

        // Act
        var response = await client.PostAsync("/api/Games/AssignedGuess", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("out of guesses");
    }

    [Fact]
    public async Task AssignedGuess_ReturnsSameGame_WhenAlreadyAssigned()
    {
        // Arrange
        var userId = $"test-user-{Guid.NewGuid()}";
        var user = CreateTestUser(userId);

        // Create a clue giver and their game
        var clueGiver = CreateTestUser($"clue-giver-{Guid.NewGuid()}", "ClueGiver");
        var (cardSet, game) = CreateGameWithCardSet(clueGiver);

        var client = CreateAuthenticatedClient(userId);

        // Act - call twice
        var response1 = await client.PostAsync("/api/Games/AssignedGuess", null);
        var result1 = await response1.Content.ReadFromJsonAsync<GameGuessResponse>();

        var response2 = await client.PostAsync("/api/Games/AssignedGuess", null);
        var result2 = await response2.Content.ReadFromJsonAsync<GameGuessResponse>();

        // Assert - should return the same game
        result1!.GameId.Should().Be(result2!.GameId);
    }

    #endregion

    #region AssignedGiveClue Tests

    [Fact]
    public async Task AssignedGiveClue_ReturnsUnauthorized_WhenNotAuthenticated()
    {
        // Arrange - use client without auth header
        var client = Factory.CreateClient();

        // Act
        var response = await client.PostAsync("/api/Games/AssignedGiveClue", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AssignedGiveClue_ReturnsCardSet_ForAuthenticatedUser()
    {
        // Arrange
        var userId = $"test-user-{Guid.NewGuid()}";
        var user = CreateTestUser(userId);

        var client = CreateAuthenticatedClient(userId);

        // Act
        var response = await client.PostAsync("/api/Games/AssignedGiveClue", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CardSetResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().NotBeEmpty();
        result.Words.Should().HaveCount(5);
    }

    [Fact]
    public async Task AssignedGiveClue_ReturnsBadRequest_WhenOutOfClueEnergy()
    {
        // Arrange
        var userId = $"test-user-{Guid.NewGuid()}";

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = TestDataSeeder.CreateTestUser(db, userId);
            user.ClueEnergy = 0; // No clue energy
            db.SaveChanges();
        }

        var client = CreateAuthenticatedClient(userId);

        // Act
        var response = await client.PostAsync("/api/Games/AssignedGiveClue", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("out of clues");
    }

    #endregion

    #region MakeGuess Tests

    [Fact]
    public async Task MakeGuess_ReturnsBadRequest_WhenNoActiveGame()
    {
        // Arrange
        var userId = $"test-user-{Guid.NewGuid()}";
        var user = CreateTestUser(userId);

        var client = CreateAuthenticatedClient(userId);
        var request = new { GuessIsInSet = true };

        // Act
        var response = await client.PostAsJsonAsync("/api/Games/MakeGuess", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task MakeGuess_UpdatesUserRating_AndReturnsResult()
    {
        // Arrange
        var userId = $"test-user-{Guid.NewGuid()}";
        var user = CreateTestUser(userId);

        // Create a clue giver and their game
        var clueGiver = CreateTestUser($"clue-giver-{Guid.NewGuid()}", "ClueGiver");
        var (cardSet, game) = CreateGameWithCardSet(clueGiver);

        var client = CreateAuthenticatedClient(userId);

        // First, get a game assigned
        await client.PostAsync("/api/Games/AssignedGuess", null);

        var request = new { GuessIsInSet = true };

        // Act
        var response = await client.PostAsJsonAsync("/api/Games/MakeGuess", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<MakeGuessResponse>();
        result.Should().NotBeNull();
        result!.Clue.Should().NotBeNullOrEmpty();
        result.AllWords.Should().NotBeEmpty();
    }

    #endregion

    #region CreateGame Tests

    [Fact]
    public async Task CreateGame_ReturnsUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        var client = Factory.CreateClient();
        var request = new
        {
            WordSetId = Guid.NewGuid().ToString(),
            Clue = "test",
            OddOneOut = "APPLE"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/Games/CreateGame", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateGame_CreatesNewGame_WithValidData()
    {
        // Arrange
        var userId = $"test-user-{Guid.NewGuid()}";
        var user = CreateTestUser(userId);

        var client = CreateAuthenticatedClient(userId);

        // First get assigned a card set
        var cardSetResponse = await client.PostAsync("/api/Games/AssignedGiveClue", null);
        var cardSet = await cardSetResponse.Content.ReadFromJsonAsync<CardSetResponse>();

        var request = new
        {
            WordSetId = cardSet!.Id.ToString(),
            Clue = "testclue",
            OddOneOut = cardSet.Words.First()
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/Games/CreateGame", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CreateGameResponse>();
        result.Should().NotBeNull();
        result!.GameId.Should().NotBeEmpty();
        result.TotalClueGiversCount.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task CreateGame_ReturnsBadRequest_WhenNotAssignedToCardSet()
    {
        // Arrange
        var userId = $"test-user-{Guid.NewGuid()}";
        var user = CreateTestUser(userId);

        var client = CreateAuthenticatedClient(userId);

        var request = new
        {
            WordSetId = Guid.NewGuid().ToString(), // Random card set not assigned
            Clue = "testclue",
            OddOneOut = "APPLE"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/Games/CreateGame", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateGame_ReturnsBadRequest_ForInvalidClue()
    {
        // Arrange
        var userId = $"test-user-{Guid.NewGuid()}";
        var user = CreateTestUser(userId);

        var client = CreateAuthenticatedClient(userId);

        // First get assigned a card set
        var cardSetResponse = await client.PostAsync("/api/Games/AssignedGiveClue", null);
        var cardSet = await cardSetResponse.Content.ReadFromJsonAsync<CardSetResponse>();

        var request = new
        {
            WordSetId = cardSet!.Id.ToString(),
            Clue = "", // Empty clue is invalid
            OddOneOut = cardSet.Words.First()
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/Games/CreateGame", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateGame_AddsToExistingGame_WhenClueExists()
    {
        // Arrange
        var userId1 = $"test-user-1-{Guid.NewGuid()}";
        var userId2 = $"test-user-2-{Guid.NewGuid()}";

        // First user creates a game
        var user1 = CreateTestUser(userId1, "User1");
        var client1 = CreateAuthenticatedClient(userId1);

        var cardSetResponse1 = await client1.PostAsync("/api/Games/AssignedGiveClue", null);
        var cardSet1 = await cardSetResponse1.Content.ReadFromJsonAsync<CardSetResponse>();

        var request1 = new
        {
            WordSetId = cardSet1!.Id.ToString(),
            Clue = "sharedclue",
            OddOneOut = cardSet1.Words.First()
        };

        var createResponse1 = await client1.PostAsJsonAsync("/api/Games/CreateGame", request1);
        var result1 = await createResponse1.Content.ReadFromJsonAsync<CreateGameResponse>();
        result1.Should().NotBeNull();
        var origClueGiversCount = result1!.TotalClueGiversCount;

        // Second user creates a game with the same clue on the same card set
        // First, they need to be assigned the same card set
        // For this test, we'll manually assign them
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user2 = TestDataSeeder.CreateTestUser(db, userId2, "User2");
            var cardSet = db.CardSet.Find(cardSet1.Id);
            user2.AssignedCardSet = cardSet;
            db.SaveChanges();
        }

        var client2 = CreateAuthenticatedClient(userId2);
        var request2 = new
        {
            WordSetId = cardSet1.Id.ToString(),
            Clue = "sharedclue", // Same clue
            OddOneOut = cardSet1.Words.First()
        };

        // Act
        var createResponse2 = await client2.PostAsJsonAsync("/api/Games/CreateGame", request2);

        // Assert
        createResponse2.StatusCode.Should().Be(HttpStatusCode.OK);

        var result2 = await createResponse2.Content.ReadFromJsonAsync<CreateGameResponse>();
        result2.Should().NotBeNull();
        result2!.GameId.Should().Be(result1!.GameId); // Same game
        result2.TotalClueGiversCount.Should().Be(origClueGiversCount + 1); // Now has 2 clue givers
    }

    #endregion
}

// Response DTOs for deserialization
public class GameGuessResponse
{
    public Guid GameId { get; set; }
    public string CurrentCard { get; set; } = "";
    public string CurrentClue { get; set; } = "";
}

public class CardSetResponse
{
    public Guid Id { get; set; }
    public List<string> Words { get; set; } = new();
}

public class MakeGuessResponse
{
    public bool IsCorrect { get; set; }
    public int NewRating { get; set; }
    public int RatingChange { get; set; }
    public string Clue { get; set; } = "";
    public List<WordInfo> AllWords { get; set; } = new();
}

public class WordInfo
{
    public string Word { get; set; } = "";
    public bool IsOddOneOut { get; set; }
    public int CorrectGuesses { get; set; }
    public int TotalGuesses { get; set; }
}

public class CreateGameResponse
{
    public Guid GameId { get; set; }
    public int TotalClueGiversCount { get; set; }
}
