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
        .HasMany(g => g.ClueGivers)
        .WithMany(u => u.CreatedGames)
        .UsingEntity(j => j.ToTable("GameClueGivers")); // Creates a cleaner join table name

    // --- 2. Configure the One-to-Many (CurrentGame <-> Users playing it) ---
    // A user has one CurrentGame. A Game has (implicitly) many users playing it.
    builder.Entity<User>()
        .HasOne(u => u.CurrentGame)
        .WithMany() // We leave this empty because Game doesn't have a "List<User> CurrentPlayers"
        .HasForeignKey(u => u.CurrentGameId);
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
}
public class Game
{
    public Guid Id { get; set; }
    public List<User> ClueGivers { get; set; } = new();
    public CardSet? CardSet { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? Clue { get; set; }
    public WordCard? OddOneOut { get; set; }
    public List<Guess> Guesses { get; set; } = new();
}
public class Guess
{
    public Guid Id { get; set; }
    public Game? Game { get; set; }
    public User? Guesser { get; set; }
    public WordCard? SelectedCard { get; set; }
    public bool GuessIsInSet { get; set; }
    public DateTime GuessedAt { get; set; } = DateTime.UtcNow;
}

public class User : IdentityUser
{
  public CardSet? AssignedCardSet { get; set; }
  public Game? CurrentGame { get; set; }
  public Guid? CurrentGameId { get; set; }
  public WordCard? CurrentCard { get; set; }
  public List<Game> CreatedGames { get; set; } = new();
  public List<Guess> Guesses { get; set; } = new();
}
