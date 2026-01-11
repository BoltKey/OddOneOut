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

      // Query games by joining with GameClueGivers to get ClueGivenAt
      var query = _context.GameClueGivers
        .Where(gcg => gcg.UserId == userIdString)
        .Include(gcg => gcg.Game)
        .ThenInclude(g => g.CardSet)
        .ThenInclude(cs => cs.WordCards)
        .Include(gcg => gcg.Game)
        .ThenInclude(g => g.Guesses)
        .ThenInclude(gg => gg.SelectedCard)
        .Include(gcg => gcg.Game)
        .ThenInclude(g => g.OddOneOut)
        .Include(gcg => gcg.Game)
        .ThenInclude(g => g.CardSet)
        .ThenInclude(cs => cs.Games)
        .OrderByDescending(gcg => gcg.ClueGivenAt);

      var totalCount = await query.CountAsync();
      var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

      // 1. Fetch the GameClueGivers with their Games and relationships
      var gameClueGivers = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .AsSplitQuery() // Recommended: prevents data explosion when including collections
        .ToListAsync();

      // 2. Project the data in memory (now we can call SuccessCoef())
      var historyData = gameClueGivers.Select(gcg => new
      {
          GameId = gcg.Game.Id,
          CardSetId = gcg.Game.CardSet.Id,
          ClueGivenAt = gcg.ClueGivenAt,
          CardSetWords = gcg.Game.CardSet.WordCards.Select(wc => new
          {
              wc.Word,
              wc.Id,
              GuessCount = gcg.Game.Guesses.Count(gg => gg.SelectedCard?.Id == wc.Id),
              CorrectGuesses = gcg.Game.Guesses.Count(gg =>
                  gg.SelectedCard?.Id == wc.Id &&
                  gg.GuessIsInSet != (gg.SelectedCard?.Id == gcg.Game.OddOneOut?.Id))
          }).ToList(),
          Clue = gcg.Game.Clue,
          OddOneOutWord = gcg.Game.OddOneOut?.Word,
          GameScore = gcg.Game.CachedGameScore, // Renamed to match frontend expectation
          OtherGamesAmt = gcg.Game.CardSet.Games.Count - 1,
          SuccessCoef = gcg.Game.SuccessCoef()
      }).ToList();

// 2. Fetch other clues for each card set (efficient batch query)
var cardSetIds = historyData.Select(d => d.CardSetId).Distinct().ToList();
var otherCluesByCardSet = await _context.GameClueGivers
    .Where(gcg => gcg.Game.CardSetId.HasValue && cardSetIds.Contains(gcg.Game.CardSetId.Value))
    .Select(gcg => new
    {
        CardSetId = gcg.Game.CardSetId!.Value,
        GameId = gcg.Game.Id,
        Clue = gcg.Game.Clue,
        OddOneOutWord = gcg.Game.OddOneOut.Word,
        ClueGivenAt = gcg.ClueGivenAt,
        CachedGameScore = gcg.Game.CachedGameScore
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
                CreatedAt = oc.ClueGivenAt,
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
        CreatedAt = d.ClueGivenAt,
        d.GameScore,
        otherGamesAmt = d.OtherGamesAmt,
        OtherClues = otherClues,
        SuccessCoef = d.SuccessCoef
    };
}).ToList();


// 3. Fetch user and recalculate rating if dirty
var user = await _context.Users.FindAsync(userIdString);
if (user != null && user.ClueRatingDirty)
{
    await _context.UpdateClueGiverRatingsAsync(new[] { userIdString });
    user.ClueRatingDirty = false;
    await _context.SaveChangesAsync();
}

return Ok(new
{
    Data = history,
    ClueRating = user?.CachedClueRating ?? 0,
    Page = page,
    PageSize = pageSize,
    TotalCount = totalCount,
    TotalPages = totalPages,
});
    }
    [HttpGet("GuessLeaderboard")]
    public async Task<IActionResult> GetAllTimeLeaderboard()
    {
        // Occasionally decay a few random inactive users (5% chance, decays 3 users)
        if (new Random().NextDouble() < 0.05)
        {
            var inactiveUsers = await _context.Users
                .Where(u => u.lastDecay < DateTime.UtcNow.AddDays(-1))
                .Take(50) // Limit to avoid loading too many users
                .ToListAsync();
            
            // Shuffle in-memory and take 3 random users
            var randomUsers = inactiveUsers
                .OrderBy(_ => Guid.NewGuid())
                .Take(3);
            
            foreach (var u in randomUsers)
            {
                u.decayRating();
            }
            await _context.SaveChangesAsync();
        }

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
        // Recalculate ratings for dirty users who might be in top positions
        // We check top 20 by cached rating + any dirty users to ensure accurate leaderboard
        var potentialTopUserIds = await _context.Users
            .Where(u => !u.IsGuest && (u.ClueRatingDirty || _context.Users
                .Where(top => !top.IsGuest)
                .OrderByDescending(top => top.CachedClueRating)
                .Take(20)
                .Select(top => top.Id)
                .Contains(u.Id)))
            .Where(u => u.ClueRatingDirty)
            .Select(u => u.Id)
            .ToListAsync();

        if (potentialTopUserIds.Count > 0)
        {
            await _context.UpdateClueGiverRatingsAsync(potentialTopUserIds);
            await _context.Users
                .Where(u => potentialTopUserIds.Contains(u.Id))
                .ExecuteUpdateAsync(u => u.SetProperty(x => x.ClueRatingDirty, false));
        }

        // Now fetch accurate leaderboard
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
            .Take(10)
            .ToListAsync();

        return Ok(topUsers);
    }
}


