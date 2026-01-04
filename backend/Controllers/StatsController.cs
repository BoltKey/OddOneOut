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

        var user = await _context.Users
            .Include(u => u.CreatedGames)
            .Include(u => u.Guesses)
            .FirstOrDefaultAsync(u => u.Id == userIdString);

        if (user == null)
        {
            return NotFound("User not found.");
        }

        var totalGamesCreated = user.CreatedGames.Count;
        var totalGuessesMade = user.Guesses.Count;
        var correctGuesses = user.Guesses.Count(g => g.Game?.OddOneOut == g.SelectedCard);

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
      if (userIdString == null)
      {
        return Unauthorized("User not authenticated.");
      }

      // Validate pagination parameters
      if (page < 1) page = 1;
      if (pageSize < 1) pageSize = 10;
      if (pageSize > 50) pageSize = 50; // Cap page size

      // Query guesses directly instead of loading the entire user object
      var query = _context.Guesses
        .Where(g => g.Guesser.Id == userIdString)
        .Include(g => g.Game)
        .ThenInclude(game => game.CardSet)
        .ThenInclude(cs => cs.WordCards)
        .OrderByDescending(g => g.GuessedAt);

      var totalCount = await query.CountAsync();
      var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

      var guesses = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();
      if (guesses.Count == 0)
      {
        return Ok(new
        {
          Data = new List<object>(),
          Page = page,
          PageSize = pageSize,
          TotalCount = totalCount,
          TotalPages = totalPages
        });
      }

      var history = guesses.Select(g => new
      {
        Game = g.Game.Id,
        CardSetId = g.Game.CardSet.Id,
        CardSetWords = g.Game.CardSet.WordCards.Select(wc => new
        {
        wc.Word,
        GuessCount = _context.Guesses.Count(
          gg => gg.SelectedCard.Id == wc.Id && gg.Game.Id == g.Game.Id),
        CorrectGuesses = _context.Guesses.Count(
          gg => gg.SelectedCard.Id == wc.Id && gg.Game.Id == g.Game.Id && gg.GuessIsInSet != (gg.SelectedCard == g.Game.OddOneOut)
        )
        }).ToList(),
        SelectedCard = g.SelectedCard.Word,
        OddOneOut = g.Game.OddOneOut.Word,
        Clue = g.Game.Clue,
        g.GuessedAt,
        g.GuessIsInSet,
        SuccessCoef = g.Game.SuccessCoef(),
        GameScore = g.Game.CachedGameScore,
        otherGamesAmt = g.Game.CardSet.Games.Count - 1,
        g.RatingChange
      }).ToList();
      _context.SaveChanges();

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

      var games = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

      var history = games.Select(g => new
      {
        Game = g.Id,
        CardSetId = g.CardSet.Id,
        CardSetWords = g.CardSet.WordCards.Select(wc => new
        {
        wc.Word,
        GuessCount = _context.Guesses.Count(
          gg => gg.SelectedCard.Id == wc.Id && gg.Game.Id == g.Id),
        CorrectGuesses = _context.Guesses.Count(
          gg => gg.SelectedCard.Id == wc.Id && gg.Game.Id == g.Id && gg.GuessIsInSet != (gg.SelectedCard == g.OddOneOut)
        )
        }).ToList(),
        g.Clue,
        OddOneOut = g.OddOneOut.Word,
        g.CreatedAt,
        SuccessCoef = g.SuccessCoef(),
        gameScore = g.CachedGameScore,
        otherGamesAmt = g.CardSet.Games.Count - 1,
      }).ToList();
      var clueScore = _context.Users
        .Where(u => u.Id == userIdString)
        .Select(u => u.CachedClueRating)
        .FirstOrDefault();
      _context.SaveChanges();

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
        var topUsers = await _context.Users
            .Where(u => !u.IsGuest)
            .OrderByDescending(u => u.GuessRating)
            .Take(10)
            .Select(u => new
            {
                u.Id,
                UserName = u.DisplayName,
                u.GuessRating,
                rank = _context.Users.Count(other => !other.IsGuest && other.GuessRating > u.GuessRating) + 1
            })
            .ToListAsync();

        return Ok(topUsers);
    }
    [HttpGet("ClueLeaderboard")]
    public async Task<IActionResult> GetClueLeaderboard()
    {
        var topUsers = await _context.Users
            .Where(u => !u.IsGuest)
            .OrderByDescending(u => u.CachedClueRating)
            .Take(1)
            .Select(u => new
            {
                u.Id,
                UserName = u.DisplayName,
                ClueRating = u.CachedClueRating,
                rank = _context.Users.Count(other => !other.IsGuest && other.CachedClueRating > u.CachedClueRating) + 1
            })
            .ToListAsync();

        return Ok(topUsers);
    }
}


