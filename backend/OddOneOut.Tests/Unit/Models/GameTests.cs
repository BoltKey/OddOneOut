using FluentAssertions;
using OddOneOut.Data;
using OddOneOut.Tests.TestFixtures;

namespace OddOneOut.Tests.Unit.Models;

/// <summary>
/// Unit tests for the Game model's scoring logic.
/// </summary>
public class GameTests
{
    public GameTests()
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
        });
    }

    private static (Game game, List<WordCard> wordCards) CreateGameWithCardSet()
    {
        var wordCards = new List<WordCard>
        {
            new WordCard { Id = Guid.NewGuid(), Word = "APPLE" },
            new WordCard { Id = Guid.NewGuid(), Word = "BANANA" },
            new WordCard { Id = Guid.NewGuid(), Word = "ORANGE" },
            new WordCard { Id = Guid.NewGuid(), Word = "GRAPE" },
            new WordCard { Id = Guid.NewGuid(), Word = "MANGO" },
        };

        var cardSet = new CardSet
        {
            Id = Guid.NewGuid(),
            WordCards = wordCards,
        };

        var game = new Game
        {
            Id = Guid.NewGuid(),
            CardSet = cardSet,
            OddOneOut = wordCards[0], // APPLE is the odd one out
            Clue = "fruit",
            Guesses = new List<Guess>(),
        };

        cardSet.Games = new List<Game> { game };

        return (game, wordCards);
    }

    private static User CreateTestUser(string id = "test-user")
    {
        return new User
        {
            Id = id,
            UserName = "TestUser",
            GuessRating = 1000,
        };
    }

    [Fact]
    public void SuccessCoef_ReturnsNull_WhenLessThanTwoGuesses()
    {
        // Arrange
        var (game, wordCards) = CreateGameWithCardSet();

        // No guesses
        game.Guesses.Clear();

        // Act
        var result = game.SuccessCoef();

        // Assert
        result.Should().BeNull();

        // Add one guess
        game.Guesses.Add(new Guess
        {
            Id = Guid.NewGuid(),
            Game = game,
            SelectedCard = wordCards[1],
            GuessIsInSet = true,
        });

        // Act again
        result = game.SuccessCoef();

        // Assert - still null with only 1 guess
        result.Should().BeNull();
    }

    [Fact]
    public void SuccessCoef_ReturnsCorrectValue_WithMultipleGuesses()
    {
        // Arrange
        var (game, wordCards) = CreateGameWithCardSet();
        var user1 = CreateTestUser("user1");
        var user2 = CreateTestUser("user2");

        // Add guesses - APPLE is the odd one out
        // Correct guess: GuessIsInSet == false when card IS the odd one out
        // Correct guess: GuessIsInSet == true when card is NOT the odd one out

        // Guess 1: BANANA (not odd one), user says "in set" = CORRECT
        game.Guesses.Add(new Guess
        {
            Id = Guid.NewGuid(),
            Game = game,
            Guesser = user1,
            SelectedCard = wordCards[1], // BANANA
            GuessIsInSet = true, // Correct - BANANA is in set
        });

        // Guess 2: APPLE (odd one), user says "not in set" = CORRECT
        game.Guesses.Add(new Guess
        {
            Id = Guid.NewGuid(),
            Game = game,
            Guesser = user2,
            SelectedCard = wordCards[0], // APPLE (odd one out)
            GuessIsInSet = false, // Correct - APPLE is not in set
        });

        // Act
        var result = game.SuccessCoef();

        // Assert - should return a value (not null) with 2+ guesses
        result.Should().NotBeNull();
        result.Should().BeGreaterThan(0);
        result.Should().BeLessThanOrEqualTo(100);
    }

    [Fact]
    public void SuccessCoef_ReturnsLowerValue_WithIncorrectGuesses()
    {
        // Arrange
        var (game, wordCards) = CreateGameWithCardSet();
        var user1 = CreateTestUser("user1");
        var user2 = CreateTestUser("user2");

        // Add incorrect guesses
        // Incorrect: APPLE (odd one), user says "in set" = WRONG
        game.Guesses.Add(new Guess
        {
            Id = Guid.NewGuid(),
            Game = game,
            Guesser = user1,
            SelectedCard = wordCards[0], // APPLE (odd one out)
            GuessIsInSet = true, // WRONG - APPLE is not in set
        });

        // Incorrect: BANANA (not odd), user says "not in set" = WRONG
        game.Guesses.Add(new Guess
        {
            Id = Guid.NewGuid(),
            Game = game,
            Guesser = user2,
            SelectedCard = wordCards[1], // BANANA
            GuessIsInSet = false, // WRONG - BANANA is in set
        });

        // Act
        var result = game.SuccessCoef();

        // Assert - should return a lower value due to incorrect guesses
        result.Should().NotBeNull();
        result.Should().BeGreaterThan(0);
        result.Should().BeLessThan(100); // Lower than perfect
    }

    [Fact]
    public void GameScore_Returns100_WhenNoSuccessCoef()
    {
        // Arrange
        var (game, _) = CreateGameWithCardSet();
        game.Guesses.Clear(); // No guesses = no SuccessCoef

        // Act
        var result = game.GameScore();

        // Assert
        result.Should().Be(100);
    }

    [Fact]
    public void GameScore_CalculatesRelativeScore_WithSiblingGames()
    {
        // Arrange
        var wordCards = new List<WordCard>
        {
            new WordCard { Id = Guid.NewGuid(), Word = "APPLE" },
            new WordCard { Id = Guid.NewGuid(), Word = "BANANA" },
            new WordCard { Id = Guid.NewGuid(), Word = "ORANGE" },
            new WordCard { Id = Guid.NewGuid(), Word = "GRAPE" },
            new WordCard { Id = Guid.NewGuid(), Word = "MANGO" },
        };

        var cardSet = new CardSet
        {
            Id = Guid.NewGuid(),
            WordCards = wordCards,
        };

        var clueGiver1 = CreateTestUser("giver1");
        var clueGiver2 = CreateTestUser("giver2");

        var game1 = new Game
        {
            Id = Guid.NewGuid(),
            CardSet = cardSet,
            OddOneOut = wordCards[0],
            Clue = "yellow",
            ClueGivers = new List<User> { clueGiver1 },
            Guesses = new List<Guess>(),
        };

        var game2 = new Game
        {
            Id = Guid.NewGuid(),
            CardSet = cardSet,
            OddOneOut = wordCards[0],
            Clue = "tropical",
            ClueGivers = new List<User> { clueGiver2 },
            Guesses = new List<Guess>(),
        };

        cardSet.Games = new List<Game> { game1, game2 };

        // Add correct guesses to game1
        for (int i = 0; i < 3; i++)
        {
            game1.Guesses.Add(new Guess
            {
                Id = Guid.NewGuid(),
                Game = game1,
                Guesser = CreateTestUser($"guesser1_{i}"),
                SelectedCard = wordCards[1], // Not the odd one
                GuessIsInSet = true, // Correct
            });
        }

        // Add incorrect guesses to game2
        for (int i = 0; i < 3; i++)
        {
            game2.Guesses.Add(new Guess
            {
                Id = Guid.NewGuid(),
                Game = game2,
                Guesser = CreateTestUser($"guesser2_{i}"),
                SelectedCard = wordCards[1], // Not the odd one
                GuessIsInSet = false, // Incorrect
            });
        }

        // Act
        var score1 = game1.GameScore();
        var score2 = game2.GameScore();

        // Assert - game1 should have better score than game2
        score1.Should().BeGreaterThan(score2);
    }

    [Fact]
    public void RecalculateScore_UpdatesCachedGameScore()
    {
        // Arrange
        var (game, wordCards) = CreateGameWithCardSet();
        game.CachedGameScore = 0; // Initial value

        // Add guesses
        for (int i = 0; i < 3; i++)
        {
            game.Guesses.Add(new Guess
            {
                Id = Guid.NewGuid(),
                Game = game,
                Guesser = CreateTestUser($"guesser_{i}"),
                SelectedCard = wordCards[1],
                GuessIsInSet = true, // Correct
            });
        }

        // Act
        game.RecalculateScore();

        // Assert
        game.CachedGameScore.Should().NotBe(0);
        game.CachedGameScore.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GameScore_CachesCachedGameScore()
    {
        // Arrange
        var (game, wordCards) = CreateGameWithCardSet();
        
        // Add guesses
        for (int i = 0; i < 3; i++)
        {
            game.Guesses.Add(new Guess
            {
                Id = Guid.NewGuid(),
                Game = game,
                Guesser = CreateTestUser($"guesser_{i}"),
                SelectedCard = wordCards[1],
                GuessIsInSet = true,
            });
        }

        // Act
        var score = game.GameScore();

        // Assert - CachedGameScore should be updated
        game.CachedGameScore.Should().Be(score);
    }
}
