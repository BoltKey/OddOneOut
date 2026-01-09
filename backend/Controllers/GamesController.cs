using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OddOneOut.Data; // Ensure this matches your namespace
using OddOneOut.Services;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")] // URL will be: localhost:xxxx/api/games
public class GamesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<GamesController> _logger;
    private readonly IWordCheckerService _wordCheckerService;

    // Inject the database connection we set up in Program.cs
    public GamesController(AppDbContext context, ILogger<GamesController> logger, IWordCheckerService wordCheckerService)
    {
        _context = context;
        _logger = logger;
        _wordCheckerService = wordCheckerService;
    }


    [HttpPost("AssignedGuess")
    , Authorize
    ]
    public async Task<IActionResult> AssignedGuess()
  {
      // find word set that hasn't been assigned to this user yet

        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _context.Users.FindAsync(userIdString);
        // check if user has assigned set
        var game = await _context.Users
        .Where(u => u.Id == userIdString)
        .Include(u => u.CurrentGame)       // 1. Load the Set
            .ThenInclude(g => g.CardSet)   // 2. Load the Cards inside the Set
          .ThenInclude(cs => cs.WordCards) // 3. Load the WordCards inside the CardSet
        .Select(u => u.CurrentGame)
        .FirstOrDefaultAsync();
        if (game == null)
        {
            // assign new game to user
            if (!user.TrySpendGuessEnergy())
            {
                return BadRequest($"You are out of guesses for now. Please wait {user.NextGuessRegenTime} before guessing again. This limit ensures the game stays interesting for everyone, and gives other clue givers time to add new games. If the limit feels too limiting, please, discuss it on the community subreddit!");
            }
            var avalGames = await _context.Games
              .Where(g => !g.ClueGivers.Any(u => u.Id == userIdString)) // Ensure user is not a ClueGiver in the game
              // ensure user hasn't guessed in any game with same word set yet
              .Where(g => !_context.Guesses.Any(gu =>
                  gu.Game.CardSet.Id == g.CardSet.Id &&
                  gu.Guesser.Id == userIdString
              ))
              .Where(g => g.Id != user.CurrentGameId && (user.AssignedCardSet == null || g.CardSetId != user.AssignedCardSet.Id))
              .Include(g => g.CardSet)   // 2. Load the Cards inside the Set
                  .ThenInclude(cs => cs.WordCards) // 3. Load the WordCards inside the CardSet
              .Include(g => g.Guesses) // Include guesses for scoring
              .OrderBy(c => Guid.NewGuid()) // Note: Can be optimized with PostgreSQL RANDOM() if needed
              .Take(GameConfig.Current.GuessAssignGamesAmt)
              .ToListAsync();
            var experience = await _context.Guesses
              .Where(gu => gu.Guesser.Id == userIdString)
              .CountAsync();
            experience = Math.Min(300, experience);
            // each game: number of other guessers, score
            // new users: prioritize games with more guesses and higher score
            // experienced users: prioritize games with fewer guesses, primarily, or higher score and more guesses secondarily
            var gameWeights = avalGames.ToDictionary(g => g, g =>
            {
              /*
              z=e^{-\frac{\left(x-200\cdot e^{-0.008a}\right)^{2}}{5000}}\cdot\frac{y+20}{120}+0.002\cdot a-\left(30-y\right)\cdot x\cdot\left(20\ +\ a\right)\cdot0.0000003-a\cdot x\cdot0.00001-\left(200-a\right)\cdot0.001
              */
                int guesses = g.Guesses.Count;
                double score = g.GameScore();

                double weight = score * 3
                + (200-experience * 3) * 0.0003*score * guesses
                + Math.Pow(experience, 2) * guesses * score * 0.000003
                + experience * (100 - guesses) * 0.05
                - (50 - score) * Math.Pow(guesses, 3) * 0.000009;

                double result = weight;
                result = Math.Max(0.001, result);
                return result;
            });

            var totalWeight = gameWeights.Values.Sum();
            var rand = new Random();
            var pick = rand.NextDouble() * totalWeight;
            Console.WriteLine($"Total Weight: {totalWeight}, Game weights: {string.Join(", ", gameWeights.Values)} Pick: {pick}");
            double cumulative = 0.0;
            foreach (var kvp in gameWeights)
            {
                cumulative += kvp.Value;
                if (pick <= cumulative)
                {
                    game = kvp.Key;
                    break;
                }
            }
            user.CurrentGame = game;
        }


        if (game == null)
        {
            return BadRequest("There are no more available games for you, try again later!");
        }
        var currentCard = user.CurrentCard;
        if (user.CurrentCard == null)
        {

            // select a card with bias to be the odd one out
            var oddOneOutChance = GameConfig.Current.OddOneOutChance;

            WordCard? randomCard;
            if (new Random().NextDouble() < oddOneOutChance)
            {
                randomCard = game.OddOneOut;
            }
            else
            {
                // get random card from set that is not the odd one out
                // Use in-memory shuffle since we already have the collection loaded
                var availableCards = game.CardSet.WordCards
                    .Where(wc => wc.Id != game.OddOneOut.Id)
                    .ToList();
                randomCard = availableCards.OrderBy(c => Guid.NewGuid()).FirstOrDefault();
            }
            if (randomCard != null)
            {
                currentCard = user.CurrentCard = randomCard;
            }
            else
              {
                return BadRequest("No available Cards found in the selected Game's CardSet.");
              }
        }
        await _context.SaveChangesAsync();
        var response = new GameGuessResponseDto
        {
            GameId = game.Id,
            CurrentCard = currentCard.Word,
            CurrentClue = game.Clue
        };

        return Ok(response);
  }

    [HttpPost("AssignedGiveClue")
    , Authorize
    ]
    public async Task<IActionResult> AssignedGiveClue()
    {
        // find word set that hasn't been assigned to this user yet

        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _context.Users.FindAsync(userIdString);
        if (user == null)
        {
            _logger.LogWarning("user not found");
            return Unauthorized("User not found.");
        }
        // check if user has assigned set
        var cardSet = await _context.Users
        .Where(u => u.Id == userIdString)
        .Include(u => u.AssignedCardSet)       // 1. Load the Set
            .ThenInclude(cs => cs.WordCards)   // 2. Load the Cards inside the Set
        .Select(u => u.AssignedCardSet)
        .FirstOrDefaultAsync();
        if (cardSet == null)
        {
          if (!user.TrySpendClueEnergy())
          {
              return BadRequest($"You are out of clues for now. Please wait {user.NextClueRegenTime} before guessing again. The limit is so low to not let players spam low-quality clues, and really think of the best clues! If the limit feels too limiting, please, discuss it on the community subreddit!");
          }
            // Optimize query: use IDs instead of object comparisons and filter in database
            var currentGameId = user.CurrentGameId;
            cardSet = await _context.CardSet
              .Where(cs => !_context.Games.Any(g =>
                  g.CardSetId == cs.Id &&           // Match the game to the card set
                  g.Id != currentGameId &&          // Exclude current game
                  (_context.Set<GameClueGiver>().Any(gcg => gcg.GameId == g.Id && gcg.UserId == userIdString) || // Check if user is a clue giver in that game
                  _context.Guesses.Any(gu => gu.GameId == g.Id && gu.Guesser.Id == userIdString)) // Check if user has guessed
              ) && _context.Set<GameClueGiver>().Count(gcg => _context.Games.Any(game => game.CardSetId == cs.Id && game.Id == gcg.GameId)) < 20) // Limit to card sets with less than 20 clue givers total
              .Include(cs => cs.WordCards)   // 2. Load the Cards inside the Set
              .OrderBy(c => Guid.NewGuid()) // Note: Can be optimized with PostgreSQL RANDOM() if needed
              .FirstOrDefaultAsync();
        }

        if (cardSet == null)
        {
            cardSet = await CardSet.CreateRandomAsync(_context);
        }
        if (cardSet == null)
        {
            _logger.LogWarning("no cardset found");
            return Unauthorized("No available CardSets found for the user.");
        }
        // Batch SaveChangesAsync calls - update user and save once
        user.AssignedCardSet = cardSet;
        await _context.SaveChangesAsync();
        var response = new CardSetResponseDto
{
            Id = cardSet.Id,
            // Extract just the word strings, or a simple object if you need IDs too
            Words = cardSet.WordCards.Select(w => w.Word).ToList()
        };

        return Ok(response);
    }
[HttpPost("MakeGuess"), Authorize]
public async Task<IActionResult> MakeGuess(MakeGuessDto request)
{
    // 1. Data Fetching (Keep this here or in a Repository)
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

    var user = await _context.Users
        .Include(u => u.CurrentCard)
        .FirstOrDefaultAsync(u => u.Id == userId);

    if (user?.CurrentCard == null) return BadRequest("No active card found.");


    // 1. Get the ID of the CardSet first (Fast lookup)
    // You might already have this from the user object, if not, fetch it lightly.
    var cardSetId = await _context.Games
      .Where(g => g.Id == user.CurrentGameId)
      .Select(g => g.CardSet.Id)
      .FirstOrDefaultAsync();

    // 2. QUERY A: Load ALL games in this set (including siblings) with their details
    // We fetch the entire batch in one flat query.
    var allGamesInSet = await _context.Games
    .Where(g => g.CardSetId == cardSetId) // Use foreign key for better performance
    .Include(g => g.OddOneOut) // Required for IsCorrect checks

    // Load ClueGivers AND the Users behind them (prevents AspNetUsers queries)
    .Include(g => g.ClueGivers)

    // Load Guesses AND the Selected Card (prevents WordCard queries)
    .Include(g => g.Guesses)
        .ThenInclude(gg => gg.SelectedCard)

    // Load CardSet and Words
    .Include(g => g.CardSet)
        .ThenInclude(cs => cs.WordCards)

    .AsSplitQuery() // Use split queries to prevent data explosion with multiple collections
    .ToListAsync();

    // 3. Find your current game in the list we just loaded
    // EF Core has already wired up all the relationships in memory!
    var game = allGamesInSet.First(g => g.Id == user.CurrentGameId);

    if (game == null) return BadRequest("Game not found.");

    // 2. Determine Context (Prepare data for the Domain)
    var isOddOneOutTarget = user.CurrentCard.Id == game.OddOneOut.Id;

    // Logic: If user guesses TRUE, they think it IS in the set (NOT the odd one)
    // So if the card IS the OddOneOut, and they guess TRUE (In Set), they are WRONG.
    // InSet means "Not Odd One Out"
    bool actualIsInSet = !isOddOneOutTarget;
    bool isCorrect = actualIsInSet == request.GuessIsInSet;

    // 3. EXECUTE BUSINESS LOGIC (The new clean line!)
    // The User entity now handles its own stats and cleanup.
    // 4. Persistence (Save the Log and the User updates)
    var guessRecord = new Guess
    {
        Id = Guid.NewGuid(),
        Game = game,       // Use ID references if possible to save DB roundtrips, but entity is fine
        Guesser = user,
        GuessedAt = DateTime.UtcNow,
        GuessIsInSet = request.GuessIsInSet,
        SelectedCard = user.CurrentCard,
        RatingChange = 0
    };
    int oldRating = user.GuessRating;
    user.ProcessGuessResult(isCorrect, isOddOneOutTarget);

    int ratingChange = user.GuessRating - oldRating;
    guessRecord.RatingChange = ratingChange;

    _context.Guesses.Add(guessRecord);

    game.RecalculateScore();
    foreach (var otherGame in game.CardSet.Games)
    {
      otherGame.RecalculateScore();
    }
    await _context.SaveChangesAsync();

    // 5. Response
    return Ok(new {
    isCorrect,
    newRating = user.GuessRating,
    ratingChange,
    clue = game.Clue,
    allWords = game.CardSet.WordCards.Select(w => new {
        Word = w.Word,
        IsOddOneOut = w.Id == game.OddOneOut.Id,
        correctGuesses = game.Guesses.Count(
            gg => gg.SelectedCard?.Id == w.Id && // Use ?.Id to be safe
            gg.GuessIsInSet != (gg.SelectedCard?.Id == game.OddOneOut?.Id)
        ),
        totalGuesses = game.Guesses.Count(
            gg => gg.SelectedCard?.Id == w.Id
        )
    })
});
}
    [HttpPost("CreateGame"), Authorize]
    public async Task<IActionResult> CreateGame(CreateGameDto request)
    {
        if (!Guid.TryParse(request.WordSetId, out var wordSetId))
        {
            return BadRequest("Invalid ID format.");
          }

        var oddOneOut = request.OddOneOut;
        if (string.IsNullOrWhiteSpace(oddOneOut))
        {
          return BadRequest("Clue cannot be empty.");
        }
        // Normalize clue to lowercase for case-insensitive matching
        var normalizedClue = request.clue?.ToLowerInvariant();
        string result = _wordCheckerService.WordInvalidReason(normalizedClue);
        if (result != null)
        {
            return BadRequest(result);
        }
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _context.Users
            .Include(u => u.AssignedCardSet)
            .FirstOrDefaultAsync(u => u.Id == userIdString);

        if (user?.AssignedCardSet?.Id != wordSetId)
        {
            return BadRequest("User is not assigned to this CardSet.");
        }


        var cardSet = await _context.CardSet.FindAsync(wordSetId);
        // 3. Check if they actually exist
        if (cardSet == null || oddOneOut == null)
        {
            return NotFound("The specified CardSet or OddOneOut card could not be found.");
        }
        // find card using word
        var oddCard = await _context.WordCard.FirstOrDefaultAsync(w => w.Word == oddOneOut);
        if (oddCard == null)
        {
            return NotFound("The specified OddOneOut card could not be found.");
        }
        // check if game with same word set and clue already exists (case-insensitive)
var existingGame = await _context.Games
    .FirstOrDefaultAsync(g => g.CardSetId == cardSet.Id && g.Clue == normalizedClue);
        CreateGameResponseDto response;
        if (existingGame != null)
        {
            // add to ClueGiver list
            await _context.Entry(existingGame)
                .Collection(g => g.ClueGivers)
                .LoadAsync();

            existingGame.ClueGivers.Add(user);
            user.AssignedCardSet = null;
            existingGame.RecalculateScore();
            // Batch SaveChangesAsync calls - save once after all updates
            await _context.SaveChangesAsync();
            response = new CreateGameResponseDto
            {
                GameId = existingGame.Id,
                ClueGiverAmt = existingGame.ClueGivers.Count
            };
            return Ok(response);
        }
        var newGame = new Game
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,

                // Map simple properties (normalized to lowercase)
                Clue = normalizedClue,

                // Map the entities we just fetched
                CardSet = cardSet,
                OddOneOut = oddCard,

                // Initialize the list (good practice, though your class already does this)
                ClueGivers = new List<User> { user }
            };
        // remove assigned game
        user.AssignedCardSet = null;

        _context.Games.Add(newGame);
        await _context.SaveChangesAsync(); // Saves to Postgres!
        response = new CreateGameResponseDto
        {
            GameId = newGame.Id,
            ClueGiverAmt = newGame.ClueGivers.Count
        };
        return Ok(response);
    }
}

// Simple object to receive data from the frontend
public class CreateGameDto
{
    public string WordSetId { get; set; }
    public string clue { get; set; }
    public string OddOneOut { get; set; }
}
public class AssignedGiveClueDto
{
}

public class CardSetResponseDto
{
    public Guid Id { get; set; }
    public List<string> Words { get; set; } = new(); // Just a list of strings! Cleaner.
}

public class GameGuessResponseDto
{
    public Guid GameId { get; set; }
    public string CurrentCard { get; set; }
    public string CurrentClue { get; set; }
}

public class MakeGuessDto
{
    public bool GuessIsInSet { get; set; }
}
public class CreateGameResponseDto
{
    public Guid GameId { get; set; }
    public int ClueGiverAmt { get; set; }
}