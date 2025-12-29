using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace OddOneOut.Data; // Ensure this namespace matches your project name

public class AppDbContext : IdentityDbContext<User>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // This line tells Postgres: "I want a table named 'Games' based on the Game class"
    public DbSet<Game> Games { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<CardSet> CardSet { get; set; }
    public DbSet<WordCard> WordCard { get; set; }
}


public class WordCard
{
    public Guid Id { get; set; }
    public string Word { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
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
    public List<string> ClueGiver { get; set; } = new();
    public CardSet CardSet { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Clue { get; set; } = string.Empty;
    public WordCard OddOneOut { get; set; } = new();
}

public class User : IdentityUser
{
  public CardSet? assignedCardSet { get; set; }
}