using OddOneOut.Data;

namespace OddOneOut.Tests.TestFixtures;

/// <summary>
/// Helper class to seed test data for integration tests.
/// </summary>
public static class TestDataSeeder
{
    /// <summary>
    /// Seeds basic data required for tests: word cards and a test user.
    /// </summary>
    public static void SeedBasicData(AppDbContext context)
    {
        // Seed word cards (required for CardSet.CreateRandomAsync)
        if (!context.WordCard.Any())
        {
            var wordCards = new List<WordCard>
            {
                new WordCard { Id = Guid.NewGuid(), Word = "APPLE", Category = "Fruit" },
                new WordCard { Id = Guid.NewGuid(), Word = "BANANA", Category = "Fruit" },
                new WordCard { Id = Guid.NewGuid(), Word = "ORANGE", Category = "Fruit" },
                new WordCard { Id = Guid.NewGuid(), Word = "GRAPE", Category = "Fruit" },
                new WordCard { Id = Guid.NewGuid(), Word = "MANGO", Category = "Fruit" },
                new WordCard { Id = Guid.NewGuid(), Word = "CHAIR", Category = "Furniture" },
                new WordCard { Id = Guid.NewGuid(), Word = "TABLE", Category = "Furniture" },
                new WordCard { Id = Guid.NewGuid(), Word = "LAMP", Category = "Furniture" },
                new WordCard { Id = Guid.NewGuid(), Word = "SOFA", Category = "Furniture" },
                new WordCard { Id = Guid.NewGuid(), Word = "DESK", Category = "Furniture" },
            };
            context.WordCard.AddRange(wordCards);
            context.SaveChanges();
        }
    }

    /// <summary>
    /// Creates a test user with default settings.
    /// </summary>
    public static User CreateTestUser(AppDbContext context, string userId, string userName = "TestUser")
    {
        // Ensure unique normalized username by appending part of the userId
        var uniqueSuffix = userId.Length > 8 ? userId.Substring(userId.Length - 8) : userId;
        var uniqueUserName = $"{userName}_{uniqueSuffix}";
        
        var user = new User
        {
            Id = userId,
            UserName = uniqueUserName,
            DisplayName = userName,
            GuessRating = GameConfig.Current.InitialGuessRating,
            GuessEnergy = GameConfig.Current.MaxGuessEnergy,
            ClueEnergy = GameConfig.Current.MaxClueEnergy,
            IsGuest = false,
            NormalizedUserName = uniqueUserName.ToUpperInvariant(),
        };

        context.Users.Add(user);
        context.SaveChanges();

        return user;
    }

    /// <summary>
    /// Creates a complete game setup: CardSet, Game, and assigns it to a user.
    /// </summary>
    public static (CardSet cardSet, Game game) CreateGameWithCardSet(
        AppDbContext context,
        User clueGiver,
        string clue = "fruit")
    {
        var wordCards = context.WordCard.Take(5).ToList();
        
        if (wordCards.Count < 5)
        {
            throw new InvalidOperationException("Not enough word cards seeded. Call SeedBasicData first.");
        }

        var cardSet = new CardSet
        {
            Id = Guid.NewGuid(),
            WordCards = wordCards,
            CreatedAt = DateTime.UtcNow,
        };

        var game = new Game
        {
            Id = Guid.NewGuid(),
            CardSet = cardSet,
            OddOneOut = wordCards.First(),
            Clue = clue.ToLowerInvariant(),
            ClueGivers = new List<User> { clueGiver },
            CreatedAt = DateTime.UtcNow,
        };

        context.CardSet.Add(cardSet);
        context.Games.Add(game);
        context.SaveChanges();

        return (cardSet, game);
    }

    /// <summary>
    /// Creates a guess for the specified game and user.
    /// </summary>
    public static Guess CreateGuess(
        AppDbContext context,
        Game game,
        User guesser,
        WordCard selectedCard,
        bool guessIsInSet)
    {
        var guess = new Guess
        {
            Id = Guid.NewGuid(),
            Game = game,
            Guesser = guesser,
            SelectedCard = selectedCard,
            GuessIsInSet = guessIsInSet,
            GuessedAt = DateTime.UtcNow,
            RatingChange = 0,
        };

        context.Guesses.Add(guess);
        game.Guesses.Add(guess);
        context.SaveChanges();

        return guess;
    }

    /// <summary>
    /// Creates multiple guesses for a game to generate meaningful statistics.
    /// </summary>
    public static List<Guess> CreateMultipleGuesses(
        AppDbContext context,
        Game game,
        int count = 5)
    {
        var guesses = new List<Guess>();
        var wordCards = game.CardSet?.WordCards ?? context.CardSet
            .Where(cs => cs.Id == game.CardSetId)
            .SelectMany(cs => cs.WordCards)
            .ToList();

        for (int i = 0; i < count; i++)
        {
            var guesser = CreateTestUser(context, $"guesser-{Guid.NewGuid()}", $"Guesser{i}");
            var selectedCard = wordCards[i % wordCards.Count];
            var isOddOneOut = selectedCard.Id == game.OddOneOut?.Id;
            
            // Alternate between correct and incorrect guesses
            var guessIsInSet = (i % 2 == 0) ? !isOddOneOut : isOddOneOut;
            
            var guess = CreateGuess(context, game, guesser, selectedCard, guessIsInSet);
            guesses.Add(guess);
        }

        return guesses;
    }
}
