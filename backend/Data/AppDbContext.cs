using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization; // Add this

namespace OddOneOut.Data; // Ensure this namespace matches your project name



public class AppDbContext : IdentityDbContext<User>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // This line tells Postgres: "I want a table named 'Games' based on the Game class"
    public DbSet<Game> Games { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<CardSet> CardSet { get; set; }
    public DbSet<WordCard> WordCard { get; set; }
    public DbSet<Guess> Guesses { get; set; }
protected override void OnModelCreating(ModelBuilder builder)
{
    base.OnModelCreating(builder); // Required for Identity

    // --- 1. Configure the Many-to-Many (ClueGivers <-> CreatedGames) ---
    builder.Entity<Game>()
        .Navigation(g => g.Guesses)
        .AutoInclude();
    builder.Entity<Game>()
        .Navigation(g => g.CardSet)
        .AutoInclude();
    builder.Entity<Game>()
        .HasMany(g => g.ClueGivers)
        .WithMany(u => u.CreatedGames)
        .UsingEntity(j => j.ToTable("GameClueGivers")); // Creates a cleaner join table name
    // --- 2. Configure the One-to-Many (CurrentGame <-> Users playing it) ---
    // A user has one CurrentGame. A Game has (implicitly) many users playing it.
    builder.Entity<User>()
        .HasOne(u => u.CurrentGame)
        .WithMany() // We leave this empty because Game doesn't have a "List<User> CurrentPlayers"
        .HasForeignKey(u => u.CurrentGameId);

    builder.Entity<User>()
        .Navigation(c => c.CreatedGames)
        .AutoInclude();
    builder.Entity<CardSet>()
        .Navigation(c => c.WordCards)
        .AutoInclude();
    builder.Entity<CardSet>()
        .Navigation(c => c.Games)
        .AutoInclude();
}
}


public class WordCard
{
    public Guid Id { get; set; }
    public string? Word { get; set; }
    public string? Category { get; set; }
    [JsonIgnore]
    public List<CardSet> CardSet { get; set; } = new();
}

public class CardSet
{
    public Guid Id { get; set; }
    public List<WordCard> WordCards { get; set; } = new();
    public List<Game> Games { get; set; } = new();
    // card set difficulty based on lowest game difficulty
    public string? Difficulty { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public static async Task<CardSet> CreateRandomAsync(AppDbContext context, int wordCount = 5)
    {
        // 1. Fetch the logic (moved from controller)
        var randomWords = await context.WordCard
            .OrderBy(c => Guid.NewGuid())
            .Take(wordCount)
            .ToListAsync();

        // 2. Instantiate the object
        var cardSet = new CardSet
        {
            Id = Guid.NewGuid(),
            WordCards = randomWords
        };

        // 3. Add to context and save
        context.CardSet.Add(cardSet);
        await context.SaveChangesAsync();

        return cardSet;
    }
}
[Index(nameof(CachedGameScore))]
public class Game
{
    public Guid Id { get; set; }
    public List<User> ClueGivers { get; set; } = new();
    public CardSet? CardSet { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? Clue { get; set; }
    public WordCard? OddOneOut { get; set; }
    public List<Guess> Guesses { get; set; } = new();
    public const int FallbackAverageSuccessCoef = 80;
    // game difficulty calculated based on number of correct guesses
    public int? SuccessCoef()
    {
      if (Guesses.Count < 2) return null;
      // for each card in card set, calculate success rate
      var cardStats = new Dictionary<Guid, (int correct, int total)>();
      foreach (var wc in CardSet!.WordCards)
      {
        cardStats[wc.Id] = (
          Guesses.Count(g => g.IsCorrect() && g.SelectedCard == wc),
          Guesses.Count(g => g.SelectedCard == wc)
        );
      }
      // geometric average of success rates
      double product = 1.0;
      foreach (var stats in cardStats.Values)
      {
        double rate = stats.total == 0 ? 1.0 : (double)(0.1 + stats.correct) / (0.1 + stats.total);
        product *= rate;
      }
      double geoMean = Math.Pow(product, 1.0 / cardStats.Count);
      return (int)(geoMean * 100);
    }
    public float CachedGameScore { get; set; } = 0;
    public float GameScore()
  {
      var successCoef = SuccessCoef();
      if (successCoef == null) return 0;
      var otherGames = CardSet?.Games.Where(g => g.Id != this.Id);
      float result;
      if (otherGames == null || otherGames.Count() == 0) {
        result = (float)(successCoef - FallbackAverageSuccessCoef);
        CachedGameScore = result;
        return result;
      }
      // average success coef of other games
      float otherAvg = (float)otherGames.Average(g => g.SuccessCoef() ?? 100);
      result = (float)(successCoef - otherAvg);
      CachedGameScore = result;
      return result;
  }
    public void RecalculateScore()
    {
      CachedGameScore = GameScore();
      foreach(var clueGiver in ClueGivers)
      {
        clueGiver.CachedClueRating = clueGiver.ClueRating();
      }

    }
    // priority based on difference between difficulty of this and easiest game
    public int? Priority { get; set; }
}
public class Guess
{
    public Guid Id { get; set; }
    public Game? Game { get; set; }
    public User? Guesser { get; set; }
    public WordCard? SelectedCard { get; set; }
    public bool GuessIsInSet { get; set; }
    public DateTime GuessedAt { get; set; } = DateTime.UtcNow;
    public int RatingChange { get; set; } = 0;
    public bool IsCorrect() => GuessIsInSet == (SelectedCard != Game?.OddOneOut);
}
[Index(nameof(GuessRating))]
[Index(nameof(CachedClueRating))]
public class User : IdentityUser
{
  public CardSet? AssignedCardSet { get; set; }
  public Game? CurrentGame { get; set; }
  public Guid? CurrentGameId { get; set; }
  public WordCard? CurrentCard { get; set; }
  public List<Game> CreatedGames { get; set; } = new();
  public List<Guess> Guesses { get; set; } = new();
  // correct guess on inset: +10
  // incorrect guess on inset: -30
  // correct guess on odd one out: +20
  // incorrect guess on odd one out: -60
  public int GuessRating { get; set; } = 1000;
  public float CachedClueRating { get; set; } = 0;
  private const float PivotRating = 1000f;
  private const int minRating = 100;
  private const int BaseRewardInset = 10;
  private const int BasePenaltyInset = 20;
  private const int BaseRewardOddOne = 20;
  private const int BasePenaltyOddOne = 50;
  public void AdjustGuessRating(int delta)
  {
    float multiplier;
    float safeRating = Math.Max(GuessRating, minRating);
    if (delta > 0)
    {
      multiplier = PivotRating / safeRating;
    }
    else
    {
      multiplier = safeRating / PivotRating;
    }
    if (delta > 0 && (int)(delta * multiplier) == 0) delta = 1;
    int finalDelta = (int)(delta * multiplier);

    GuessRating += finalDelta;
    if (GuessRating < minRating) GuessRating = minRating; // Prevent ratings below minimum
  }
  public void ProcessGuessResult(bool isCorrect, bool isOddOneOutTarget)
    {
      // 1. Calculate Base Points based on your specific rules
      int baseDelta = 0;

      if (isOddOneOutTarget)
      {
        baseDelta = isCorrect ? BaseRewardOddOne : -BasePenaltyOddOne;
      }
      else // It was an Inset card
      {
        baseDelta = isCorrect ? BaseRewardInset : -BasePenaltyInset;
      }

      // 2. Apply the "Elo-like" Scaling (The logic we discussed earlier)
      AdjustGuessRating(baseDelta);

      // 3. Clear the user's state (The cleanup logic)
      CurrentCard = null;
      CurrentGame = null;
    }
  public float ClueRating()
  {
      var result = 1000f;
      foreach (var g in CreatedGames)
      {
          result += g.GameScore();
      }
      return result;
  }
}
