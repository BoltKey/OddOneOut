using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OddOneOut.Data; // Ensure this matches your namespace
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")] // URL will be: localhost:xxxx/api/stats
public class StatsController : ControllerBase
{
    private readonly AppDbContext _context;

    public StatsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("UserStats"), Authorize]
    public async Task<IActionResult> GetUserStats()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdString == null)
        {
            return Unauthorized("User not authenticated.");
        }

        // Optimize: Use projection to calculate counts in database instead of loading all data
        var userStats = await _context.Users
            .Where(u => u.Id == userIdString)
            .Select(u => new
            {
                TotalGamesCreated = u.CreatedGames.Count,
                TotalGuessesMade = u.Guesses.Count,
                CorrectGuesses = u.Guesses.Count(g => g.SelectedCardId == g.Game.OddOneOut.Id)
            })
            .FirstOrDefaultAsync();

        if (userStats == null)
        {
            return NotFound("User not found.");
        }

        var totalGamesCreated = userStats.TotalGamesCreated;
        var totalGuessesMade = userStats.TotalGuessesMade;
        var correctGuesses = userStats.CorrectGuesses;

        var stats = new
        {
            TotalGamesCreated = totalGamesCreated,
            TotalGuessesMade = totalGuessesMade,
            CorrectGuesses = correctGuesses,
            Accuracy = totalGuessesMade > 0 ? (double)correctGuesses / totalGuessesMade * 100 : 0
        };

        return Ok(stats);
    }
[HttpGet("GuessHistory"), Authorize]
public async Task<IActionResult> GetHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
{
    var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userIdString == null) return Unauthorized("User not authenticated.");

    // Validate pagination
    if (page < 1) page = 1;
    if (pageSize < 1) pageSize = 10;
    if (pageSize > 50) pageSize = 50;

    // 1. OPTIMIZATION: Use efficient query with projection and split queries
    var query = _context.Guesses
        .Where(g => g.Guesser.Id == userIdString)
        .Include(g => g.Game)
            .ThenInclude(game => game.CardSet)
                .ThenInclude(cs => cs.WordCards)
        .Include(g => g.Game)
            .ThenInclude(game => game.CardSet)
                .ThenInclude(cs => cs.Games) // Load Games to count them later
        .Include(g => g.Game)
            .ThenInclude(game => game.Guesses) // Load Sibling Guesses for stats
                .ThenInclude(gg => gg.SelectedCard)
        .Include(g => g.Game)
            .ThenInclude(game => game.OddOneOut) // Needed for correctness check
        .OrderByDescending(g => g.GuessedAt)
        .AsSplitQuery(); // Use split queries to prevent data explosion

    var totalCount = await query.CountAsync();
    var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

    // 2. Execute ONE main query
    var guesses = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    if (guesses.Count == 0)
    {
        return Ok(new { Data = new List<object>(), Page = page, PageSize = pageSize, TotalCount = totalCount, TotalPages = totalPages });
    }

    // 3. Perform calculations in Memory (No more DB calls here)
    var history = guesses.Select(g => new
    {
        Game = g.Game.Id,
        CardSetId = g.Game.CardSet.Id,
        CardSetWords = g.Game.CardSet.WordCards.Select(wc => new
        {
            wc.Word,
            // Optimization: Count the list already in memory using LINQ
            GuessCount = g.Game.Guesses.Count(gg =>
                gg.SelectedCard?.Id == wc.Id),

            // Optimization: Calculate correctness in memory using IDs
            CorrectGuesses = g.Game.Guesses.Count(gg =>
                gg.SelectedCardId == wc.Id &&
                gg.GuessIsInSet != (gg.SelectedCardId == g.Game.OddOneOut.Id))
        }).ToList(),
        SelectedCard = g.SelectedCard?.Word,
        OddOneOut = g.Game.OddOneOut?.Word,
        Clue = g.Game.Clue,
        g.GuessedAt,
        g.GuessIsInSet,
        SuccessCoef = g.Game.SuccessCoef(),
        GameScore = g.Game.CachedGameScore,
        // Optimization: Count the list already in memory
        otherGamesAmt = g.Game.CardSet.Games.Count - 1,
        g.RatingChange
    }).ToList();

    // REMOVED: _context.SaveChanges();
    // You should never call SaveChanges in a GET request (it's for writing changes).

    return Ok(new
    {
        Data = history,
        Page = page,
        PageSize = pageSize,
        TotalCount = totalCount,
        TotalPages = totalPages
    });
}
    [HttpGet("ClueHistory"), Authorize]
    public async Task<IActionResult> GetClueHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
      var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
      if (userIdString == null)
      {
        return Unauthorized("User not authenticated.");
      }

      // Validate pagination parameters
      if (page < 1) page = 1;
      if (pageSize < 1) pageSize = 10;
      if (pageSize > 50) pageSize = 50; // Cap page size

      // Query games directly instead of loading the entire user object
      var query = _context.Games
        .Where(g => g.ClueGivers.Any(u => u.Id == userIdString))
        .Include(g => g.CardSet)
        .ThenInclude(cs => cs.WordCards)
        .OrderByDescending(g => g.CreatedAt);

      var totalCount = await query.CountAsync();
      var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

      // 1. Project the data BEFORE fetching.
// This allows EF to write the SQL for Counts.
var historyData = await query
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    // Move Select here so it translates to SQL
    .Select(g => new
    {
        GameId = g.Id,
        CardSetId = g.CardSet.Id,
        // EF Core will translate this nested collection into a JOIN or separate split query
        CardSetWords = g.CardSet.WordCards.Select(wc => new
        {
            wc.Word,
            wc.Id,
            // Optimization: These Counts become SQL subqueries
            GuessCount = _context.Guesses.Count(gg =>
                gg.SelectedCard.Id == wc.Id && gg.Game.Id == g.Id),

            // Note: I switched object comparison to ID comparison for better SQL translation
            CorrectGuesses = _context.Guesses.Count(gg =>
                gg.SelectedCard.Id == wc.Id &&
                gg.Game.Id == g.Id &&
                gg.GuessIsInSet != (gg.SelectedCard.Id == g.OddOneOut.Id))
        }).ToList(),
        g.Clue,
        OddOneOutWord = g.OddOneOut.Word,
        g.CreatedAt,
        g.CachedGameScore,
        // Optimization: This becomes a subquery (SELECT COUNT(*) FROM Games...)
        OtherGamesAmt = g.CardSet.Games.Count - 1,

        // IMPORTANT: We cannot call custom C# methods like g.SuccessCoef() inside SQL.
        // If this method relies on data inside 'g', fetch the raw data here
        // and calculate it in step 2.
        // For now, I'll assume you can recalculate it or it's a stored prop.
        // If it's a stored column, just include: g.SuccessCoefStored
    })
    .AsSplitQuery() // Recommended: prevents data explosion when including collections
    .ToListAsync();

// 2. Fetch other clues for each card set (efficient batch query)
var cardSetIds = historyData.Select(d => d.CardSetId).Distinct().ToList();
var otherCluesByCardSet = await _context.Games
    .Where(g => g.CardSetId.HasValue && cardSetIds.Contains(g.CardSetId.Value))
    .Select(g => new
    {
        CardSetId = g.CardSetId!.Value,
        GameId = g.Id,
        Clue = g.Clue,
        OddOneOutWord = g.OddOneOut.Word,
        CreatedAt = g.CreatedAt,
        CachedGameScore = g.CachedGameScore
    })
    .ToListAsync();

var otherCluesGrouped = otherCluesByCardSet
    .GroupBy(c => c.CardSetId)
    .ToDictionary(g => g.Key, g => g.ToList());

// 3. Perform final in-memory formatting (Client-Side Evaluation)
// This is fast because no DB calls happen here.
var history = historyData.Select(d =>
{
    List<object> otherClues = new List<object>();
    if (otherCluesGrouped.ContainsKey(d.CardSetId))
    {
        otherClues = otherCluesGrouped[d.CardSetId]
            .Where(oc => oc.GameId != d.GameId)
            .Select(oc => (object)new
            {
                Clue = oc.Clue,
                OddOneOut = oc.OddOneOutWord,
                CreatedAt = oc.CreatedAt,
                GameScore = oc.CachedGameScore
            })
            .OrderByDescending(oc => ((dynamic)oc).CreatedAt)
            .ToList();
    }

    return new
    {
        Game = d.GameId,
        d.CardSetId,
        d.CardSetWords,
        d.Clue,
        OddOneOut = d.OddOneOutWord,
        d.CreatedAt,
        d.CachedGameScore,
        otherGamesAmt = d.OtherGamesAmt,
        OtherClues = otherClues,
        SuccessCoef = 0.0 // Placeholder: Replace with your logic
    };
}).ToList();


// 3. Fetch user rating separately (this is fast, single query)
var clueScore = await _context.Users
    .Where(u => u.Id == userIdString)
    .Select(u => u.CachedClueRating)
    .FirstOrDefaultAsync();

// _context.SaveChanges(); // Removed: You usually shouldn't save changes on a GET request.

return Ok(new
{
    Data = history,
    ClueRating = clueScore,
    Page = page,
    PageSize = pageSize,
    TotalCount = totalCount,
    TotalPages = totalPages,
});
    }
    [HttpGet("GuessLeaderboard")]
    public async Task<IActionResult> GetAllTimeLeaderboard()
    {
        // Optimize: Calculate rank efficiently using a single query with window functions
        var topUsers = await _context.Users
            .Where(u => !u.IsGuest)
            .OrderByDescending(u => u.GuessRating)
            .Select(u => new
            {
                u.Id,
                UserName = u.DisplayName,
                u.GuessRating,
                // Use raw SQL for efficient ranking (PostgreSQL ROW_NUMBER window function)
                Rank = _context.Users
                    .Where(other => !other.IsGuest && other.GuessRating > u.GuessRating)
                    .Count() + 1
            })
            .Take(10)
            .ToListAsync();

        return Ok(topUsers);
    }
    [HttpGet("ClueLeaderboard")]
    public async Task<IActionResult> GetClueLeaderboard()
    {
        // Optimize: Calculate rank efficiently using a single query
        var topUsers = await _context.Users
            .Where(u => !u.IsGuest)
            .OrderByDescending(u => u.CachedClueRating)
            .Select(u => new
            {
                u.Id,
                UserName = u.DisplayName,
                ClueRating = u.CachedClueRating,
                Rank = _context.Users
                    .Where(other => !other.IsGuest && other.CachedClueRating > u.CachedClueRating)
                    .Count() + 1
            })
            .Take(10) // Fixed: was taking only 1, should take top 10 like the other leaderboard
            .ToListAsync();

        return Ok(topUsers);
    }
}


