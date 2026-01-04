using System.Collections.Frozen; // Requires .NET 8
using DotnetBadWordDetector;

namespace OddOneOut.Services
{
public class WordCheckerService : IWordCheckerService
{
    private readonly FrozenSet<string> _validWords;
    private readonly ProfanityDetector _profanityDetector;

    public WordCheckerService(IWebHostEnvironment env)
    {
        // 1. Load Dictionary
        // Setup a text file "csw_words.txt" in your wwwroot or Data folder
        var path = Path.Combine(env.ContentRootPath, "Data", "possibleClues.txt");

        // Reading lines is fast; FrozenSet optimizes memory layout for read speed
        var words = File.ReadAllLines(path)
            .Select(w => w.Trim().ToUpperInvariant());

        _validWords = words.ToFrozenSet();

        // 2. Initialize Profanity Detector (ML-based, local)
        _profanityDetector = new ProfanityDetector();
    }

    public string WordInvalidReason(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "Word is empty.";

        var word = input.ToUpperInvariant();

        // Step 1: Dictionary Check (Instant)
        if (!_validWords.Contains(word))
        {
            return "Word is not in the dictionary (negative words like anti- un- non- are not allowed)."; // Not a real word
        }

        // Step 2: Safety Check (Local ML)
        // Returns true if it looks like profanity (e.g. "sh!t")
        if (_profanityDetector.IsProfane(input))
        {
            return "No profanities please."; // Valid Scrabble word, but blocked by safety filter
        }

        return null;
    }
}
}