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
        
        // Check if file exists to prevent crashing
        if (!File.Exists(path))
        {
            logger?.LogError("WordCards file not found at {Path}", path);
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
            logger?.LogWarning("No words found in possibleCards.txt");
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
            // Silent when everything is already seeded
            logger?.LogDebug("All {Count} words already exist in database", wordsFromFile.Count);
            return;
        }

        // Create WordCard entities for new words only
        var entities = newWords.Select(word => new WordCard
        {
            Id = Guid.NewGuid(),
            Category = "word",
            Word = word
        }).ToList();

        // Add only new words to database
        context.WordCard.AddRange(entities);
        context.SaveChanges();
        
        logger?.LogInformation("Added {Count} new WordCards. Total: {Total}", entities.Count, existingWords.Count + entities.Count);
    }
}