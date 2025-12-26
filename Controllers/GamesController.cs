using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OddOneOut.Data; // Ensure this matches your namespace

[ApiController]
[Route("api/[controller]")] // URL will be: localhost:xxxx/api/games
public class GamesController : ControllerBase
{
    private readonly AppDbContext _context;

    // Inject the database connection we set up in Program.cs
    public GamesController(AppDbContext context)
    {
        _context = context;
    }

    // 1. POST: Create a new game
    [HttpPost]
    public async Task<IActionResult> CreateGame(CreateGameDto request)
    {
        var newGame = new Game
        {
            Id = Guid.NewGuid(),
            Player1Name = request.Player1Name,
            // We initialize with an empty board or default state
            GameStateJson = "{ \"tiles\": [], \"turn\": 1 }"
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
    public string Player1Name { get; set; }
}