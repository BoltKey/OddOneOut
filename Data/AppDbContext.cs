using Microsoft.EntityFrameworkCore;

namespace OddOneOut.Data; // Ensure this namespace matches your project name

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // This line tells Postgres: "I want a table named 'Games' based on the Game class"
    public DbSet<Game> Games { get; set; }
}

// This is the actual data structure for the table
public class Game
{
    public Guid Id { get; set; }
    public string Player1Name { get; set; } = string.Empty;
    public string Player2Name { get; set; } = string.Empty;
    public string GameStateJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}