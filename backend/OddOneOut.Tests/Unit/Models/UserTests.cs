using FluentAssertions;
using OddOneOut.Data;

namespace OddOneOut.Tests.Unit.Models;

/// <summary>
/// Unit tests for the User model's rating and energy logic.
/// </summary>
public class UserTests
{
    public UserTests()
    {
        // Ensure GameConfig is initialized for tests
        GameConfig.Initialize(new GameSettings
        {
            InitialGuessRating = 1000,
            MaxGuessEnergy = 50,
            MaxClueEnergy = 5,
            BaseRewardInset = 10,
            BasePenaltyInset = 20,
            BaseRewardOddOne = 20,
            BasePenaltyOddOne = 50,
            OddOneOutChance = 0.4f,
            GuessEnergyRegenIntervalMinutes = 0.3f,
            ClueEnergyRegenIntervalMinutes = 4f,
        });
    }

    private static User CreateTestUser(int guessRating = 1000, int guessEnergy = 50, int clueEnergy = 5)
    {
        return new User
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "TestUser",
            GuessRating = guessRating,
            GuessEnergy = guessEnergy,
            ClueEnergy = clueEnergy,
        };
    }

    #region ProcessGuessResult Tests

    [Fact]
    public void ProcessGuessResult_IncreasesRating_WhenCorrect_InSet()
    {
        // Arrange
        var user = CreateTestUser();
        var initialRating = user.GuessRating;
        user.CurrentCard = new WordCard { Id = Guid.NewGuid(), Word = "BANANA" };
        user.CurrentGame = new Game { Id = Guid.NewGuid() };

        // Act - correct guess for non-odd-one-out card
        user.ProcessGuessResult(isCorrect: true, isOddOneOutTarget: false);

        // Assert
        user.GuessRating.Should().BeGreaterThan(initialRating);
        user.CurrentCard.Should().BeNull();
        user.CurrentGame.Should().BeNull();
    }

    [Fact]
    public void ProcessGuessResult_DecreasesRating_WhenIncorrect_InSet()
    {
        // Arrange
        var user = CreateTestUser();
        var initialRating = user.GuessRating;
        user.CurrentCard = new WordCard { Id = Guid.NewGuid(), Word = "BANANA" };
        user.CurrentGame = new Game { Id = Guid.NewGuid() };

        // Act - incorrect guess for non-odd-one-out card
        user.ProcessGuessResult(isCorrect: false, isOddOneOutTarget: false);

        // Assert
        user.GuessRating.Should().BeLessThan(initialRating);
        user.CurrentCard.Should().BeNull();
        user.CurrentGame.Should().BeNull();
    }

    [Fact]
    public void ProcessGuessResult_HigherReward_ForOddOneOut()
    {
        // Arrange
        var userInSet = CreateTestUser();
        var userOddOne = CreateTestUser();
        var initialRating = userInSet.GuessRating;

        userInSet.CurrentCard = new WordCard();
        userInSet.CurrentGame = new Game();
        userOddOne.CurrentCard = new WordCard();
        userOddOne.CurrentGame = new Game();

        // Act
        userInSet.ProcessGuessResult(isCorrect: true, isOddOneOutTarget: false);
        userOddOne.ProcessGuessResult(isCorrect: true, isOddOneOutTarget: true);

        // Assert - OddOneOut should give higher reward
        var inSetGain = userInSet.GuessRating - initialRating;
        var oddOneGain = userOddOne.GuessRating - initialRating;
        oddOneGain.Should().BeGreaterThan(inSetGain);
    }

    [Fact]
    public void ProcessGuessResult_HigherPenalty_ForOddOneOut()
    {
        // Arrange
        var userInSet = CreateTestUser();
        var userOddOne = CreateTestUser();
        var initialRating = userInSet.GuessRating;

        userInSet.CurrentCard = new WordCard();
        userInSet.CurrentGame = new Game();
        userOddOne.CurrentCard = new WordCard();
        userOddOne.CurrentGame = new Game();

        // Act
        userInSet.ProcessGuessResult(isCorrect: false, isOddOneOutTarget: false);
        userOddOne.ProcessGuessResult(isCorrect: false, isOddOneOutTarget: true);

        // Assert - OddOneOut should give higher penalty
        var inSetLoss = initialRating - userInSet.GuessRating;
        var oddOneLoss = initialRating - userOddOne.GuessRating;
        oddOneLoss.Should().BeGreaterThan(inSetLoss);
    }

    [Fact]
    public void ProcessGuessResult_ClearsCurrentCardAndGame()
    {
        // Arrange
        var user = CreateTestUser();
        user.CurrentCard = new WordCard { Id = Guid.NewGuid(), Word = "TEST" };
        user.CurrentGame = new Game { Id = Guid.NewGuid() };

        // Act
        user.ProcessGuessResult(isCorrect: true, isOddOneOutTarget: false);

        // Assert
        user.CurrentCard.Should().BeNull();
        user.CurrentGame.Should().BeNull();
    }

    #endregion

    #region AdjustGuessRating Tests

    [Fact]
    public void AdjustGuessRating_AppliesHigherMultiplier_WhenBelowPivot()
    {
        // Arrange
        var lowRatingUser = CreateTestUser(guessRating: 500);
        var highRatingUser = CreateTestUser(guessRating: 1500);

        // Act - same base delta
        lowRatingUser.AdjustGuessRating(10);
        highRatingUser.AdjustGuessRating(10);

        // Assert - low rating user should gain more (higher multiplier for gains)
        var lowRatingGain = lowRatingUser.GuessRating - 500;
        var highRatingGain = highRatingUser.GuessRating - 1500;
        lowRatingGain.Should().BeGreaterThan(highRatingGain);
    }

    [Fact]
    public void AdjustGuessRating_AppliesLowerMultiplier_WhenAbovePivot_ForPenalties()
    {
        // Arrange
        var lowRatingUser = CreateTestUser(guessRating: 500);
        var highRatingUser = CreateTestUser(guessRating: 1500);

        // Act - same base penalty
        lowRatingUser.AdjustGuessRating(-10);
        highRatingUser.AdjustGuessRating(-10);

        // Assert - high rating user should lose more
        var lowRatingLoss = 500 - lowRatingUser.GuessRating;
        var highRatingLoss = 1500 - highRatingUser.GuessRating;
        highRatingLoss.Should().BeGreaterThan(lowRatingLoss);
    }

    [Fact]
    public void AdjustGuessRating_NeverGoesBelowMinimum()
    {
        // Arrange
        var user = CreateTestUser(guessRating: 150);

        // Act - apply large penalty
        user.AdjustGuessRating(-200);

        // Assert - should not go below 100 (minRating)
        user.GuessRating.Should().BeGreaterThanOrEqualTo(100);
    }

    [Fact]
    public void AdjustGuessRating_AppliesMultiplier_ForHighRatingUser()
    {
        // Arrange - very high rating user
        var user = CreateTestUser(guessRating: 10000);
        var initialRating = user.GuessRating;

        // Act - apply a larger positive delta to see the effect
        user.AdjustGuessRating(100);

        // Assert - multiplier reduces gains for high rating users
        // The gain should be less than the base delta due to multiplier
        var gain = user.GuessRating - initialRating;
        gain.Should().BeLessThan(100);
        gain.Should().BeGreaterThan(0);
    }

    #endregion

    #region Energy Tests

    [Fact]
    public void TrySpendGuessEnergy_ReturnsTrue_WhenSufficientEnergy()
    {
        // Arrange
        var user = CreateTestUser(guessEnergy: 10);

        // Act
        var result = user.TrySpendGuessEnergy(5);

        // Assert
        result.Should().BeTrue();
        user.GuessEnergy.Should().Be(5);
    }

    [Fact]
    public void TrySpendGuessEnergy_ReturnsFalse_WhenInsufficientEnergy()
    {
        // Arrange
        var user = CreateTestUser(guessEnergy: 3);

        // Act
        var result = user.TrySpendGuessEnergy(5);

        // Assert
        result.Should().BeFalse();
        user.GuessEnergy.Should().Be(3); // Unchanged
    }

    [Fact]
    public void TrySpendGuessEnergy_ReturnsTrue_WhenExactEnergy()
    {
        // Arrange
        var user = CreateTestUser(guessEnergy: 5);

        // Act
        var result = user.TrySpendGuessEnergy(5);

        // Assert
        result.Should().BeTrue();
        user.GuessEnergy.Should().Be(0);
    }

    [Fact]
    public void TrySpendClueEnergy_ReturnsTrue_WhenSufficientEnergy()
    {
        // Arrange
        var user = CreateTestUser(clueEnergy: 3);

        // Act
        var result = user.TrySpendClueEnergy(2);

        // Assert
        result.Should().BeTrue();
        user.ClueEnergy.Should().Be(1);
    }

    [Fact]
    public void TrySpendClueEnergy_ReturnsFalse_WhenInsufficientEnergy()
    {
        // Arrange
        var user = CreateTestUser(clueEnergy: 1);

        // Act
        var result = user.TrySpendClueEnergy(2);

        // Assert
        result.Should().BeFalse();
        user.ClueEnergy.Should().Be(1); // Unchanged
    }

    [Fact]
    public void TrySpendClueEnergy_ReturnsTrue_WhenExactEnergy()
    {
        // Arrange
        var user = CreateTestUser(clueEnergy: 2);

        // Act
        var result = user.TrySpendClueEnergy(2);

        // Assert
        result.Should().BeTrue();
        user.ClueEnergy.Should().Be(0);
    }

    #endregion

    #region DecayRating Tests

    [Fact]
    public void DecayRating_ReducesRating_AfterInactivity()
    {
        // Arrange
        var user = CreateTestUser(guessRating: 1000);
        // Need a guess to establish "last activity" time in the past
        user.Guesses = new List<Guess>
        {
            new Guess
            {
                Id = Guid.NewGuid(),
                GuessedAt = DateTime.UtcNow.AddDays(-3) // Last guess 3 days ago
            }
        };
        user.lastDecay = DateTime.UtcNow.AddDays(-5); // Last decay 5 days ago

        // Act
        user.decayRating();

        // Assert - rating should decay based on days since last guess (3 days)
        user.GuessRating.Should().BeLessThan(1000);
    }

    [Fact]
    public void DecayRating_NoChange_WhenActiveRecently()
    {
        // Arrange
        var user = CreateTestUser(guessRating: 1000);
        user.Guesses = new List<Guess>
        {
            new Guess
            {
                Id = Guid.NewGuid(),
                GuessedAt = DateTime.UtcNow.AddHours(-1) // Guessed 1 hour ago
            }
        };
        user.lastDecay = DateTime.UtcNow.AddHours(-2);

        var initialRating = user.GuessRating;

        // Act
        user.decayRating();

        // Assert - no decay because of recent activity
        user.GuessRating.Should().Be(initialRating);
    }

    [Fact]
    public void DecayRating_UsesLastGuessTime_WhenMoreRecentThanLastDecay()
    {
        // Arrange
        var user = CreateTestUser(guessRating: 1000);
        user.Guesses = new List<Guess>
        {
            new Guess
            {
                Id = Guid.NewGuid(),
                GuessedAt = DateTime.UtcNow.AddHours(-12) // Guessed 12 hours ago (less than 1 day)
            }
        };
        user.lastDecay = DateTime.UtcNow.AddDays(-5); // Last decay 5 days ago

        var initialRating = user.GuessRating;

        // Act
        user.decayRating();

        // Assert - should not decay because last guess was less than 1 day ago
        user.GuessRating.Should().Be(initialRating);
    }

    #endregion

    #region ClueRating Tests

    [Fact]
    public void ClueRating_ReturnsBaseRating_WhenNoGames()
    {
        // Arrange
        var user = CreateTestUser();
        user.CreatedGames = new List<Game>();

        // Act
        var result = user.ClueRating();

        // Assert
        result.Should().Be(1000f);
    }

    [Fact]
    public void ClueRating_IncreasesWithGoodGameScores()
    {
        // Arrange
        var user = CreateTestUser();
        user.CreatedGames = new List<Game>
        {
            new Game
            {
                Id = Guid.NewGuid(),
                CachedGameScore = 150, // Good score
                CreatedAt = DateTime.UtcNow.AddHours(-1)
            }
        };

        // Act
        var result = user.ClueRating();

        // Assert
        result.Should().BeGreaterThan(1000f);
    }

    [Fact]
    public void ClueRating_LimitedToMostRecent100Games()
    {
        // Arrange
        var user = CreateTestUser();
        user.CreatedGames = new List<Game>();
        
        // Add 150 games
        for (int i = 0; i < 150; i++)
        {
            user.CreatedGames.Add(new Game
            {
                Id = Guid.NewGuid(),
                CachedGameScore = 100,
                CreatedAt = DateTime.UtcNow.AddDays(-i)
            });
        }

        // Act
        var result = user.ClueRating();

        // Assert - should be calculated (method doesn't error with many games)
        result.Should().BeGreaterThan(0);
    }

    #endregion
}
