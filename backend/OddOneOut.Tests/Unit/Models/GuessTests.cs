using FluentAssertions;
using OddOneOut.Data;

namespace OddOneOut.Tests.Unit.Models;

/// <summary>
/// Unit tests for the Guess model's correctness logic.
/// </summary>
public class GuessTests
{
    private static (Game game, WordCard oddOneOut, WordCard normalCard) CreateGameWithCards()
    {
        var oddOneOut = new WordCard { Id = Guid.NewGuid(), Word = "APPLE" };
        var normalCard = new WordCard { Id = Guid.NewGuid(), Word = "BANANA" };
        
        var game = new Game
        {
            Id = Guid.NewGuid(),
            OddOneOut = oddOneOut,
            Clue = "yellow",
        };

        return (game, oddOneOut, normalCard);
    }

    [Fact]
    public void IsCorrect_ReturnsTrue_WhenGuessInSetAndNotOddOne()
    {
        // Arrange
        var (game, oddOneOut, normalCard) = CreateGameWithCards();
        
        var guess = new Guess
        {
            Id = Guid.NewGuid(),
            Game = game,
            SelectedCard = normalCard, // Not the odd one out
            GuessIsInSet = true, // User says "in set"
        };

        // Act
        var result = guess.IsCorrect();

        // Assert - Correct! BANANA is in set, user guessed "in set"
        result.Should().BeTrue();
    }

    [Fact]
    public void IsCorrect_ReturnsTrue_WhenGuessNotInSetAndIsOddOne()
    {
        // Arrange
        var (game, oddOneOut, normalCard) = CreateGameWithCards();
        
        var guess = new Guess
        {
            Id = Guid.NewGuid(),
            Game = game,
            SelectedCard = oddOneOut, // The odd one out
            GuessIsInSet = false, // User says "not in set"
        };

        // Act
        var result = guess.IsCorrect();

        // Assert - Correct! APPLE is the odd one, user guessed "not in set"
        result.Should().BeTrue();
    }

    [Fact]
    public void IsCorrect_ReturnsFalse_WhenGuessInSetButIsOddOne()
    {
        // Arrange
        var (game, oddOneOut, normalCard) = CreateGameWithCards();
        
        var guess = new Guess
        {
            Id = Guid.NewGuid(),
            Game = game,
            SelectedCard = oddOneOut, // The odd one out
            GuessIsInSet = true, // User says "in set" - WRONG!
        };

        // Act
        var result = guess.IsCorrect();

        // Assert - Incorrect! APPLE is the odd one, but user guessed "in set"
        result.Should().BeFalse();
    }

    [Fact]
    public void IsCorrect_ReturnsFalse_WhenGuessNotInSetAndNotOddOne()
    {
        // Arrange
        var (game, oddOneOut, normalCard) = CreateGameWithCards();
        
        var guess = new Guess
        {
            Id = Guid.NewGuid(),
            Game = game,
            SelectedCard = normalCard, // Not the odd one out
            GuessIsInSet = false, // User says "not in set" - WRONG!
        };

        // Act
        var result = guess.IsCorrect();

        // Assert - Incorrect! BANANA is in set, but user guessed "not in set"
        result.Should().BeFalse();
    }

    [Fact]
    public void IsCorrect_HandlesNullGame()
    {
        // Arrange
        var normalCard = new WordCard { Id = Guid.NewGuid(), Word = "BANANA" };
        
        var guess = new Guess
        {
            Id = Guid.NewGuid(),
            Game = null, // Game is null
            SelectedCard = normalCard,
            GuessIsInSet = true,
        };

        // Act - should not throw
        var result = guess.IsCorrect();

        // Assert - when Game is null, Game?.OddOneOut is null
        // GuessIsInSet == (SelectedCard != null) -> true == true -> true
        result.Should().BeTrue();
    }

    [Fact]
    public void IsCorrect_HandlesNullSelectedCard()
    {
        // Arrange
        var (game, oddOneOut, normalCard) = CreateGameWithCards();
        
        var guess = new Guess
        {
            Id = Guid.NewGuid(),
            Game = game,
            SelectedCard = null, // No card selected
            GuessIsInSet = true,
        };

        // Act - should not throw
        var result = guess.IsCorrect();

        // Assert - null != oddOneOut is true, so "in set" for null card is true
        // This is an edge case that shouldn't happen in normal gameplay
        result.Should().BeTrue();
    }

    [Fact]
    public void IsCorrect_WorksWithDifferentCardIds()
    {
        // Arrange - test that comparison uses object reference/Id correctly
        var oddOneOut = new WordCard { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Word = "APPLE" };
        var anotherApple = new WordCard { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Word = "APPLE" }; // Same word, different ID
        
        var game = new Game
        {
            Id = Guid.NewGuid(),
            OddOneOut = oddOneOut,
            Clue = "red",
        };

        var guess = new Guess
        {
            Id = Guid.NewGuid(),
            Game = game,
            SelectedCard = anotherApple, // Different card object (different ID), same word
            GuessIsInSet = true,
        };

        // Act
        var result = guess.IsCorrect();

        // Assert - anotherApple is NOT the OddOneOut (different object), so it's "in set"
        // User guessed "in set", which is correct
        result.Should().BeTrue();
    }
}
