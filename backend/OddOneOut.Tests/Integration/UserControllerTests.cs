using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OddOneOut.Data;
using OddOneOut.Tests.TestFixtures;
using System.Net;
using System.Net.Http.Json;

namespace OddOneOut.Tests.Integration;

/// <summary>
/// Integration tests for the UserController API endpoints.
/// </summary>
public class UserControllerTests : IntegrationTestBase
{
    public UserControllerTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    #region GetMe Tests

    [Fact]
    public async Task GetMe_ReturnsUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        var client = Factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/User/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMe_ReturnsUserProfile_WhenAuthenticated()
    {
        // Arrange
        var userId = $"test-user-{Guid.NewGuid()}";
        var user = CreateTestUser(userId, "TestUserName");
        var client = CreateAuthenticatedClient(userId);

        // Act
        var response = await client.GetAsync("/api/User/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<UserProfileResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(userId);
        result.DisplayName.Should().Be("TestUserName"); // DisplayName preserves original name
        result.UserName.Should().StartWith("TestUserName"); // UserName has unique suffix
        result.GuessRating.Should().Be(GameConfig.Current.InitialGuessRating);
        result.GuessEnergy.Should().Be(GameConfig.Current.MaxGuessEnergy);
        result.ClueEnergy.Should().Be(GameConfig.Current.MaxClueEnergy);
        result.MaxGuessEnergy.Should().Be(GameConfig.Current.MaxGuessEnergy);
        result.MaxClueEnergy.Should().Be(GameConfig.Current.MaxClueEnergy);
    }

    [Fact]
    public async Task GetMe_ReturnsNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = $"nonexistent-user-{Guid.NewGuid()}";
        var client = CreateAuthenticatedClient(userId);

        // Act
        var response = await client.GetAsync("/api/User/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMe_ReturnsCorrectRanks()
    {
        // Arrange
        // Create users with different ratings
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            var user1 = TestDataSeeder.CreateTestUser(db, $"user1-{Guid.NewGuid()}", "User1");
            user1.GuessRating = 1500;
            
            var user2 = TestDataSeeder.CreateTestUser(db, $"user2-{Guid.NewGuid()}", "User2");
            user2.GuessRating = 1200;
            
            var user3 = TestDataSeeder.CreateTestUser(db, $"user3-{Guid.NewGuid()}", "User3");
            user3.GuessRating = 1800;
            
            db.SaveChanges();
        }

        var targetUserId = $"target-user-{Guid.NewGuid()}";
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var targetUser = TestDataSeeder.CreateTestUser(db, targetUserId, "TargetUser");
            targetUser.GuessRating = 1300; // Should rank below 1500 and 1800
            db.SaveChanges();
        }

        var client = CreateAuthenticatedClient(targetUserId);

        // Act
        var response = await client.GetAsync("/api/User/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<UserProfileResponse>();
        result.Should().NotBeNull();
        result!.GuessRank.Should().BeGreaterThan(1); // Not first place
    }

    #endregion

    #region ChangeDisplayName Tests

    [Fact]
    public async Task ChangeDisplayName_ReturnsUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        var client = Factory.CreateClient();
        var request = new { NewDisplayName = "NewName" };

        // Act
        var response = await client.PostAsJsonAsync("/api/User/changeDisplayName", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangeDisplayName_UpdatesName_ForAuthenticatedUser()
    {
        // Arrange
        var userId = $"test-user-{Guid.NewGuid()}";
        var user = CreateTestUser(userId, "OldName");
        var client = CreateAuthenticatedClient(userId);
        var request = new { NewDisplayName = "NewDisplayName" };

        // Act
        var response = await client.PostAsJsonAsync("/api/User/changeDisplayName", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify the name was actually changed
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var updatedUser = await db.Users.FindAsync(userId);
        updatedUser!.DisplayName.Should().Be("NewDisplayName");
    }

    [Fact]
    public async Task ChangeDisplayName_ReturnsNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = $"nonexistent-user-{Guid.NewGuid()}";
        var client = CreateAuthenticatedClient(userId);
        var request = new { NewDisplayName = "NewName" };

        // Act
        var response = await client.PostAsJsonAsync("/api/User/changeDisplayName", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region CreateGuest Tests

    [Fact]
    public async Task CreateGuest_CreatesNewGuestUser()
    {
        // Arrange
        var client = Factory.CreateClient();

        // Act
        var response = await client.PostAsync("/api/User/create-guest", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<CreateGuestResponse>();
        result.Should().NotBeNull();
        result!.UserId.Should().NotBeNullOrEmpty();

        // Verify the user was created as a guest
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var guestUser = await db.Users.FindAsync(result.UserId);
        guestUser.Should().NotBeNull();
        guestUser!.IsGuest.Should().BeTrue();
    }

    [Fact]
    public async Task CreateGuest_ReusesExistingGuest_WhenIdProvided()
    {
        // Arrange
        var client = Factory.CreateClient();

        // First, create a guest
        var response1 = await client.PostAsync("/api/User/create-guest", null);
        var result1 = await response1.Content.ReadFromJsonAsync<CreateGuestResponse>();
        var guestId = result1!.UserId;

        // Act - Create another client and try to reuse the guest
        var client2 = Factory.CreateClient();
        var response2 = await client2.PostAsync($"/api/User/create-guest?guestUserId={guestId}", null);

        // Assert
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result2 = await response2.Content.ReadFromJsonAsync<CreateGuestResponse>();
        result2.Should().NotBeNull();
        result2!.UserId.Should().Be(guestId); // Same user ID
    }

    [Fact]
    public async Task CreateGuest_CreatesNewGuest_WhenProvidedIdNotFound()
    {
        // Arrange
        var client = Factory.CreateClient();
        var fakeGuestId = Guid.NewGuid().ToString();

        // Act
        var response = await client.PostAsync($"/api/User/create-guest?guestUserId={fakeGuestId}", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<CreateGuestResponse>();
        result.Should().NotBeNull();
        result!.UserId.Should().NotBe(fakeGuestId); // New user created
    }

    #endregion

    #region Login/Signup Tests (Basic validation)

    [Fact]
    public async Task Login_ReturnsUnauthorized_WithInvalidCredentials()
    {
        // Arrange
        var client = Factory.CreateClient();
        var request = new
        {
            Username = "nonexistent",
            Password = "wrongpassword"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/User/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Signup_ReturnsBadRequest_WhenPasswordTooShort()
    {
        // Arrange
        var client = Factory.CreateClient();
        var request = new
        {
            Username = $"newuser{Guid.NewGuid()}",
            Password = "123" // Too short (minimum is 4)
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/User/signup", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Signup_CreatesNewUser_WithValidCredentials()
    {
        // Arrange
        var client = Factory.CreateClient();
        var username = $"newuser{Guid.NewGuid()}";
        var request = new
        {
            Username = username,
            Password = "validpassword"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/User/signup", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify user was created
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = db.Users.FirstOrDefault(u => u.UserName == username);
        user.Should().NotBeNull();
        user!.IsGuest.Should().BeFalse();
    }

    #endregion

    #region RedditLogin Tests

    [Fact]
    public async Task RedditLogin_ReturnsBadRequest_WhenRedditUserIdEmpty()
    {
        // Arrange
        var client = Factory.CreateClient();
        var request = new
        {
            RedditUserId = "",
            RedditUsername = "TestUser"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/User/reddit-login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RedditLogin_ReturnsBadRequest_WhenInvalidRedditIdFormat()
    {
        // Arrange
        var client = Factory.CreateClient();
        var request = new
        {
            RedditUserId = "invalid_format", // Should start with "t2_"
            RedditUsername = "TestUser"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/User/reddit-login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RedditLogin_CreatesNewUser_WithValidRedditId()
    {
        // Arrange
        var client = Factory.CreateClient();
        var redditId = $"t2_{Guid.NewGuid().ToString().Substring(0, 8)}";
        var request = new
        {
            RedditUserId = redditId,
            RedditUsername = "RedditTestUser"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/User/reddit-login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<RedditLoginResponse>();
        result.Should().NotBeNull();
        result!.UserId.Should().NotBeNullOrEmpty();
        result.IsNew.Should().BeTrue();

        // Verify user was created with Reddit ID
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.FindAsync(result.UserId);
        user.Should().NotBeNull();
        user!.RedditUserId.Should().Be(redditId);
        user.IsGuest.Should().BeFalse();
    }

    [Fact]
    public async Task RedditLogin_ReusesExistingUser_WithSameRedditId()
    {
        // Arrange
        var client = Factory.CreateClient();
        var redditId = $"t2_{Guid.NewGuid().ToString().Substring(0, 8)}";
        var request = new
        {
            RedditUserId = redditId,
            RedditUsername = "RedditTestUser"
        };

        // First login
        var response1 = await client.PostAsJsonAsync("/api/User/reddit-login", request);
        var result1 = await response1.Content.ReadFromJsonAsync<RedditLoginResponse>();

        // Second login with same Reddit ID
        var client2 = Factory.CreateClient();
        var response2 = await client2.PostAsJsonAsync("/api/User/reddit-login", request);

        // Assert
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result2 = await response2.Content.ReadFromJsonAsync<RedditLoginResponse>();
        result2.Should().NotBeNull();
        result2!.UserId.Should().Be(result1!.UserId); // Same user
        result2.IsNew.Should().BeFalse();
    }

    #endregion
}

// Response DTOs
public class UserProfileResponse
{
    public string Id { get; set; } = "";
    public string? UserName { get; set; }
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public int GuessRating { get; set; }
    public float ClueRating { get; set; }
    public int GuessEnergy { get; set; }
    public int ClueEnergy { get; set; }
    public int MaxGuessEnergy { get; set; }
    public int MaxClueEnergy { get; set; }
    public int GuessRank { get; set; }
    public int ClueRank { get; set; }
    public bool IsGuest { get; set; }
    public DateTime? NextGuessRegenTime { get; set; }
    public DateTime? NextClueRegenTime { get; set; }
    public bool CanGiveClues { get; set; }
    public int GuessesToGiveClues { get; set; }
}

public class CreateGuestResponse
{
    public string UserId { get; set; } = "";
}

public class RedditLoginResponse
{
    public string UserId { get; set; } = "";
    public bool IsNew { get; set; }
    public bool Linked { get; set; }
}
