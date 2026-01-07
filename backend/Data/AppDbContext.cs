using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations.Schema;
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
    .UsingEntity<GameClueGiver>(
        j => j
            .HasOne(gc => gc.User)
            .WithMany()
            .HasForeignKey(gc => gc.UserId), // <--- CHANGE: Use UserId, not User
        j => j
            .HasOne(gc => gc.Game)
            .WithMany()
            .HasForeignKey(gc => gc.GameId), // <--- CHANGE: Use GameId, not Game
        j =>
        {
            j.Property(pt => pt.ClueGivenAt).HasDefaultValueSql("now()");

            // CHANGE: Use IDs for the composite key
            j.HasKey(t => new { t.GameId, t.UserId });

            j.ToTable("GameClueGivers");
        });
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
[Index(nameof(ClueGivenAt))]
public class GameClueGiver
{
    public Game Game { get; set; }
    public Guid GameId { get; set; }

    public User User { get; set; }
    public string UserId { get; set; }

    // This is the property you want
    public DateTime ClueGivenAt { get; set; }
}
[Index(nameof(CachedGameScore))]
public class Game
{
    public Guid Id { get; set; }
    public int? PresetId { get; set; }
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
      // weighed average by number of cluegivers of success coef of other games
      float totalWeight = 0;
      foreach (var g in otherGames)
      {
        totalWeight += g.ClueGivers.Count;
      }
      float weightedSum = 0;
      foreach (var g in otherGames)
      {
        weightedSum += (g.SuccessCoef() ?? 100) * g.ClueGivers.Count;
      }
      float otherAvg = totalWeight == 0 ? 0 : weightedSum / totalWeight;
      result = 100 + (float)(successCoef - otherAvg);
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
[Index(nameof(GuessedAt))]
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
public class GameSettings
{
    // These defaults act as a fallback if config is missing
    public int InitialGuessRating { get; set; } = 1000;
    public int MaxGuessEnergy { get; set; } = 50;
    public int MaxClueEnergy { get; set; } = 5;
    public int BaseRewardInset { get; set; } = 10;
    public int BasePenaltyInset { get; set; } = 20;
    public int BaseRewardOddOne { get; set; } = 20;
    public int BasePenaltyOddOne { get; set; } = 50;
    public float OddOneOutChance { get; set; } = 0.4f;
    public float GuessEnergyRegenIntervalMinutes { get; set; } = 0.3f;
    public float ClueEnergyRegenIntervalMinutes { get; set; } = 4f;
    public int MinGuessesToGiveClues { get; set; } = 2;
    public int GuessAssignGamesAmt { get; set; } = 100;
}
public static class GameConfig
{
    // This will hold the actual loaded settings
    public static GameSettings Current { get; private set; } = new GameSettings();

    // We call this once in Program.cs to "inject" the values
    public static void Initialize(GameSettings settings)
    {
        Current = settings;
    }
}
[Index(nameof(GuessRating))]
[Index(nameof(CachedClueRating))]

public class User : IdentityUser
{
    public bool IsGuest { get; set; } = false;
    // --- Backing Fields (The actual storage) ---
    // We explicitly define these so we can manipulate them in the Getters
    private int _guessEnergy;
    private int _clueEnergy;
    private DateTime _lastGuessEnergyRegen = DateTime.UtcNow;
    private DateTime _lastClueEnergyRegen = DateTime.UtcNow;
    public string? DisplayName { get; set; }
    public int GuessEnergy
    {
        get => GetRegeneratedEnergy(ref _guessEnergy, ref _lastGuessEnergyRegen, GameConfig.Current.GuessEnergyRegenIntervalMinutes, GameConfig.Current.MaxGuessEnergy);
        set => SetEnergy(ref _guessEnergy, ref _lastGuessEnergyRegen, value);
    }

    public int ClueEnergy
    {
        get => GetRegeneratedEnergy(ref _clueEnergy, ref _lastClueEnergyRegen, GameConfig.Current.ClueEnergyRegenIntervalMinutes, GameConfig.Current.MaxClueEnergy);
        set => SetEnergy(ref _clueEnergy, ref _lastClueEnergyRegen, value);
    }

    public DateTime LastGuessEnergyRegen
    {
        get => GetRegeneratedDate(ref _guessEnergy, ref _lastGuessEnergyRegen, GameConfig.Current.GuessEnergyRegenIntervalMinutes, GameConfig.Current.MaxGuessEnergy);
        set => _lastGuessEnergyRegen = value;
    }

    public DateTime LastClueEnergyRegen
    {
        get => GetRegeneratedDate(ref _clueEnergy, ref _lastClueEnergyRegen, GameConfig.Current.ClueEnergyRegenIntervalMinutes, GameConfig.Current.MaxClueEnergy);
        set => _lastClueEnergyRegen = value;
    }

    // --- Computed UI Properties ---

    [NotMapped]
    public DateTime? NextGuessRegenTime => GetNextRegenTime(GuessEnergy, LastGuessEnergyRegen, GameConfig.Current.GuessEnergyRegenIntervalMinutes, GameConfig.Current.MaxGuessEnergy);

    [NotMapped]
    public DateTime? NextClueRegenTime => GetNextRegenTime(ClueEnergy, LastClueEnergyRegen, GameConfig.Current.ClueEnergyRegenIntervalMinutes, GameConfig.Current.MaxClueEnergy);

    // --- Functional Helpers (The Logic Core) ---

    // Helper 1: Handles the Getter for Energy
    private int GetRegeneratedEnergy(ref int current, ref DateTime last, float interval, int max)
    {
        RegenerateEnergy(ref current, ref last, interval, max);
        return current;
    }

    // Helper 2: Handles the Getter for Dates
    private DateTime GetRegeneratedDate(ref int current, ref DateTime last, float interval, int max)
    {
        RegenerateEnergy(ref current, ref last, interval, max);
        return last;
    }

    // Helper 3: Handles the Setter
    private void SetEnergy(ref int current, ref DateTime last, int value)
    {
        current = value;
        // Reset the timer whenever energy is manually set (refilled/consumed)
        last = DateTime.UtcNow;
    }

    // Helper 4: Handles Next Regen Calculation
    private DateTime? GetNextRegenTime(int current, DateTime last, float interval, int max)
    {
        if (current >= max) return null;
        return last.AddMinutes(interval);
    }

    // Helper 5: The Actual Math (Your existing logic)
    private void RegenerateEnergy(ref int currentEnergy, ref DateTime lastRegen, float intervalMinutes, int max)
    {
        if (currentEnergy >= max)
        {
            lastRegen = DateTime.UtcNow;
            return;
        }

        var timePassed = DateTime.UtcNow - lastRegen;
        if (timePassed.TotalMinutes >= intervalMinutes)
        {
            int pointsGained = (int)(timePassed.TotalMinutes / intervalMinutes);
            if (pointsGained > 0)
            {
                currentEnergy += pointsGained;
                lastRegen = lastRegen.AddMinutes(pointsGained * intervalMinutes);

                if (currentEnergy >= max)
                {
                    currentEnergy = max;
                    lastRegen = DateTime.UtcNow;
                }
            }
        }
    }

    // --- Existing Navigation Properties ---
    public CardSet? AssignedCardSet { get; set; }
    public Game? CurrentGame { get; set; }
    public Guid? CurrentGameId { get; set; }
    public WordCard? CurrentCard { get; set; }
    public List<Game> CreatedGames { get; set; } = new();
    public List<Guess> Guesses { get; set; } = new();

    // --- Existing Rating Logic ---
    public int GuessRating { get; set; } = 1000;
    public float CachedClueRating { get; set; } = 0;

    private const float PivotRating = 1000f;
    private const int minRating = 100;

    public void AdjustGuessRating(int delta)
    {
        float multiplier;
        float safeRating = Math.Max(GuessRating, minRating);

        if (delta > 0) multiplier = PivotRating / safeRating;
        else multiplier = safeRating / PivotRating;

        if (delta > 0 && (int)(delta * multiplier) == 0) delta = 1;
        int finalDelta = (int)(delta * multiplier);

        GuessRating += finalDelta;
        if (GuessRating < minRating) GuessRating = minRating;
    }

    public void ProcessGuessResult(bool isCorrect, bool isOddOneOutTarget)
    {
        int baseDelta = 0;
        if (isOddOneOutTarget)
            baseDelta = isCorrect ? GameConfig.Current.BaseRewardOddOne : -GameConfig.Current.BasePenaltyOddOne;
        else
            baseDelta = isCorrect ? GameConfig.Current.BaseRewardInset : -GameConfig.Current.BasePenaltyInset;

        AdjustGuessRating(baseDelta);

        CurrentCard = null;
        CurrentGame = null;
    }
    public bool TrySpendGuessEnergy(int amount = 1)
    {
        // Access the property (GuessEnergy) to ensure regen triggers first!
        if (GuessEnergy >= amount)
        {
            // We modify the backing field directly to avoid triggering the setter logic (which resets the timer)
            _guessEnergy -= amount;
            return true;
        }
        return false;
    }
    public bool TrySpendClueEnergy(int amount = 1)
    {
        if (ClueEnergy >= amount)
        {
            _clueEnergy -= amount;
            return true;
        }
        return false;
    }

    public float ClueRating()
    {
        var result = 1000f;
        // Added null check '?' just in case EF didn't load the collection
        if (CreatedGames != null)
        {
            foreach (var g in CreatedGames)
            {
                result += g.GameScore();
            }
        }
        return result;
    }
    public void decayRating()
      {
        DateTime lastGuessTime = Guesses.OrderByDescending(g => g.GuessedAt).FirstOrDefault()?.GuessedAt ?? DateTime.UtcNow;
        var daysInactive = (DateTime.UtcNow - lastGuessTime).TotalDays;
        GuessRating -= (int)daysInactive;
      }
}

