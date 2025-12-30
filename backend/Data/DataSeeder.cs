using OddOneOut.Data;
using Microsoft.AspNetCore.Hosting; // Needed for IWebHostEnvironment
using System.IO; // Needed for Path

public static class DataSeeder
{
    // 1. Add IWebHostEnvironment env to the parameters
    public static void SeedWordCards(AppDbContext context, IWebHostEnvironment env)
    {
        if (context.WordCard.Any()) return;

        // 2. Use env.ContentRootPath to find the project folder dynamically
        // Note: Make sure the file is actually inside a "Data" folder in your project
        var path = Path.Combine(env.ContentRootPath, "Data", "possibleCards.txt");
        Console.WriteLine($"Seeding WordCards from: {path}");
        // 3. Check if file exists to prevent crashing
        if (!File.Exists(path))
        {
            Console.WriteLine($"ERROR: File not found at {path}");
            return;
        }

        // 4. Use the 'path' variable, not the hardcoded string
        var words = File.ReadAllText(path)
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.Trim())
            .Where(w => !string.IsNullOrEmpty(w))
            .ToList();

        var entities = new List<WordCard>();

        foreach (var word in words)
        {
            entities.Add(new WordCard
            {
                Id = Guid.NewGuid(),
                Category = "word",
                Word = word
            });
        }

        context.WordCard.AddRange(entities);
        context.SaveChanges();
    }
}