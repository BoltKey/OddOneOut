using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OddOneOut.Data; // Ensure this matches your namespace
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")] // URL will be: localhost:xxxx/api/games
public class GamesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<GamesController> _logger;

    // Inject the database connection we set up in Program.cs
    public GamesController(AppDbContext context, ILogger<GamesController> logger)
    {
        _context = context;
        _logger = logger;
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
            game = await _context.Games
              .Where(g => !g.ClueGivers.Any(u => u.Id == userIdString)) // Ensure user is not a ClueGiver in the game
              // ensure user hasn't guessed in this game yet
              .Where(g => !_context.Guesses.Any(gu =>
                  gu.Game.Id == g.Id &&
                  gu.PlayerId == userIdString
              ))
              .OrderBy(c => Guid.NewGuid())
              .Include(g => g.CardSet)   // 2. Load the Cards inside the Set
                  .ThenInclude(cs => cs.WordCards) // 3. Load the WordCards inside the CardSet
              .FirstOrDefaultAsync();
            user.CurrentGame = game;

        }


        if (game == null)
        {
            _logger.LogWarning("no game found");
            return Unauthorized("No available Games found for the user.");
        }
        var currentCard = user.CurrentCard;
        if (user.CurrentCard == null)
        {
            // randomly select a card from the set
            var randomCard = game.CardSet.WordCards
                .OrderBy(c => Guid.NewGuid())
                .FirstOrDefault();
            if (randomCard != null)
            {
                currentCard = user.CurrentCard = randomCard;
            }
            else
              {
                _logger.LogWarning("no card found in set");
                return Unauthorized("No available Cards found in the selected Game's CardSet.");
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
            cardSet = await _context.CardSet
              .Where(cs => !_context.Games.Any(g =>
                  g.CardSet.Id == cs.Id &&           // Match the game to the card set
                  g.ClueGivers.Contains(user) // Check if user is in that game
              ))
              .OrderBy(c => Guid.NewGuid())
              .Include(cs => cs.WordCards)   // 2. Load the Cards inside the Set
              .FirstOrDefaultAsync();
        }

        if (cardSet == null)
        {
            // create new card set of 5 random words for user and assign to user
            var randomWords = await _context.WordCard
                .OrderBy(c => Guid.NewGuid())
                .Take(5)
                .ToListAsync();
            cardSet = new CardSet
            {
                Id = Guid.NewGuid(),
                WordCards = randomWords
            };
            _context.CardSet.Add(cardSet);
            await _context.SaveChangesAsync();
        }
        if (cardSet == null)
        {
            _logger.LogWarning("no cardset found");
            return Unauthorized("No available CardSets found for the user.");
        }
      await _context.SaveChangesAsync();
        _context.Users
            .Where(u => u.Id == userIdString)
            .ToList()
            .ForEach(u => u.AssignedCardSet = cardSet);

        await _context.SaveChangesAsync();
        var response = new CardSetResponseDto
{
            Id = cardSet.Id,
            // Extract just the word strings, or a simple object if you need IDs too
            Words = cardSet.WordCards.Select(w => w.Word).ToList()
        };

        return Ok(response);
    }
    [HttpPost("createGame"), Authorize]
    public async Task<IActionResult> CreateGame(CreateGameDto request)
    {
        if (!Guid.TryParse(request.WordSetId, out var wordSetId))
        {
            return BadRequest("Invalid ID format.");
          }

          var oddOneOut = request.OddOneOut;
          if (string.IsNullOrWhiteSpace(oddOneOut))
          {
            return BadRequest("OddOneOut cannot be empty.");
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
        // check if game with same word set and clue already exists
        var existingGame = await _context.Games
            .Include(g => g.CardSet)
            .FirstOrDefaultAsync(g => g.CardSet.Id == cardSet.Id && g.Clue == request.clue);
        if (existingGame != null)
        {
            // add to ClueGiver list
            existingGame.ClueGivers.Add(user);
            await _context.SaveChangesAsync();
            return Ok(existingGame);
        }
        var newGame = new Game
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,

                // Map simple properties
                Clue = request.clue,

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

        return Ok(newGame);
    }

    // 2. GET: Load a game by ID
    [HttpGet("{id}")]
    public async Task<IActionResult> GetGame(Guid id)
    {
        var game = await _context.Games.FindAsync(id);

        if (game == null) return NotFound();

        return Ok(game);
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