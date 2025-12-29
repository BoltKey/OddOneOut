using Microsoft.AspNetCore.Authorization;
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

    // 1. POST: Create a new game
    [HttpGet("startCreatingGame")
    , Authorize
    ]
    public async Task<IActionResult> StartCreatingGame()
    {
        // find word set that hasn't been assigned to this user yet

        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        // check if user has assigned set
        var cardSet = await _context.Users
            .Where(u => u.Id == userIdString)
            .Select(u => u.assignedCardSet)
            .FirstOrDefaultAsync();
        if (cardSet == null)
        {
            cardSet = await _context.CardSet
              .Where(cs => !_context.Games.Any(g =>
                  g.CardSet.Id == cs.Id &&           // Match the game to the card set
                  g.ClueGiver.Contains(userIdString) // Check if user is in that game
              ))
              .OrderBy(c => Guid.NewGuid())          // PostgreSQL: ORDER BY RANDOM()
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
        }
        if (cardSet == null)
        {
            _logger.LogWarning("no cardset found");
            return Unauthorized("No available CardSets found for the user.");
        }
      /*await _context.SaveChangesAsync();
        _context.Users
            .Where(u => u.Id == userIdString)
            .ToList()
            .ForEach(u => u.assignedCardSet = cardSet);*/

        await _context.SaveChangesAsync();

        return Ok(cardSet);
    }
    [HttpPost("createGame")]
    public async Task<IActionResult> CreateGame(CreateGameDto request)
    {
        if (!Guid.TryParse(request.WordSetId, out var wordSetId) ||
        !Guid.TryParse(request.OddOneOutId, out var oddOneOutId))
        {
            return BadRequest("Invalid ID format.");
        }
        var cardSet = await _context.CardSet.FindAsync(wordSetId);
        var oddOneOut = await _context.WordCard.FindAsync(oddOneOutId);
        // 3. Check if they actually exist
        if (cardSet == null || oddOneOut == null)
        {
            return NotFound("The specified CardSet or OddOneOut card could not be found.");
        }
        var newGame = new Game
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,

                // Map simple properties
                Clue = request.clue,

                // Map the entities we just fetched
                CardSet = cardSet,
                OddOneOut = oddOneOut,

                // Initialize the list (good practice, though your class already does this)
                ClueGiver = new List<string>()
            };

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
    public string OddOneOutId { get; set; }
}
public class StartCreatingGameDto
{
}