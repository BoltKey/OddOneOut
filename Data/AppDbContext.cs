using Microsoft.EntityFrameworkCore;

namespace OddOneOut.Data; // Ensure this namespace matches your project name

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // This line tells Postgres: "I want a table named 'Games' based on the Game class"
    public DbSet<Game> Games { get; set; }
    public DbSet<User> Users { get; set; }
}

// This is the actual data structure for the table
public class Game
{
    public Guid Id { get; set; }
    public string Player1Name { get; set; } = string.Empty;
    public string GameStateJson { get; set; } = "{}";
    public List<WordCard> WordCards { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class WordCard
{
    public Guid Id { get; set; }
    public string Word { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public List<Game> Games { get; set; } = new();
}

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
}