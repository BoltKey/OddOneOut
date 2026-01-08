using OddOneOut.Data;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

public static class DataSeeder
{
    public static void SeedWordCards(AppDbContext context, IWebHostEnvironment env, ILogger? logger = null)
    {
        // Find the word list file
        var path = Path.Combine(env.ContentRootPath, "Data", "possibleCards.txt");
        logger?.LogInformation($"Seeding WordCards from: {path}");
        Console.WriteLine($"Seeding WordCards from: {path}");
        
        // Check if file exists to prevent crashing
        if (!File.Exists(path))
        {
            var errorMsg = $"ERROR: File not found at {path}";
            logger?.LogError(errorMsg);
            Console.WriteLine(errorMsg);
            return;
        }

        // Read and parse words from file
        var wordsFromFile = File.ReadAllText(path)
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.Trim())
            .Where(w => !string.IsNullOrEmpty(w))
            .Distinct() // Remove any duplicates in file
            .ToList();

        if (wordsFromFile.Count == 0)
        {
            var errorMsg = "No words found in possibleCards.txt";
            logger?.LogWarning(errorMsg);
            Console.WriteLine(errorMsg);
            return;
        }

        // Get existing words from database (case-insensitive comparison)
        var existingWords = context.WordCard
            .Select(w => w.Word.ToLower())
            .ToHashSet();

        // Find words that are in the file but not in the database
        var newWords = wordsFromFile
            .Where(w => !existingWords.Contains(w.ToLower()))
            .ToList();

        if (newWords.Count == 0)
        {
            var msg = $"All {wordsFromFile.Count} words from file already exist in database. No new words to add.";
            logger?.LogInformation(msg);
            Console.WriteLine(msg);
            return;
        }

        // Create WordCard entities for new words only
        var entities = new List<WordCard>();
        foreach (var word in newWords)
        {
            entities.Add(new WordCard
            {
                Id = Guid.NewGuid(),
                Category = "word",
                Word = word
            });
        }

        // Add only new words to database
        context.WordCard.AddRange(entities);
        context.SaveChanges();
        
        var successMsg = $"Successfully added {entities.Count} new WordCards to database. Total words in database: {existingWords.Count + entities.Count}";
        logger?.LogInformation(successMsg);
        Console.WriteLine(successMsg);
    }
}