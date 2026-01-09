using System.Collections.Frozen; // Requires .NET 8
using DotnetBadWordDetector;

namespace OddOneOut.Services
{
public class WordCheckerService : IWordCheckerService
{
    private readonly FrozenSet<string> _validWords;
    private readonly ProfanityDetector _profanityDetector;

    // Whitelist of words that are commonly false-positived by the profanity detector
    // These are legitimate words that happen to contain substrings matching profanity patterns
    private static readonly FrozenSet<string> _profanityWhitelist = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        // Words containing "f*ck" patterns
        "FLICKER", "FLICKERING", "FLICKERED", "FLICKERS", "FLICKERINGLY",
        // Words containing "c*ck" patterns
        "COCK", "COCKED", "COCKING", "COCKS", "COCKY", "COCKTAIL", "COCKTAILS",
        "COCKPIT", "COCKPITS", "COCKEREL", "COCKERELS", "COCKATOO", "COCKATOOS",
        "PEACOCK", "PEACOCKS", "GAMECOCK", "GAMECOCKS", "STOPCOCK", "STOPCOCKS",
        // Words containing "t*t" patterns
        "TITLE", "TITLES", "TITLED", "TITLING", "TITULAR",
        "TITAN", "TITANS", "TITANIC", "TITANIUM",
        "TITIVATE", "TITIVATED", "TITIVATES", "TITIVATING",
        // Words containing "a*s" patterns
        "ASSASSIN", "ASSASSINS", "ASSASSINATE", "ASSASSINATED", "ASSASSINATION",
        "BASS", "BASSES", "BASSIST", "BASSISTS",
        "CLASS", "CLASSES", "CLASSIC", "CLASSICAL", "CLASSIFY",
        "GRASS", "GRASSES", "GRASSY",
        "MASS", "MASSES", "MASSIVE", "MASSIVELY",
        "PASS", "PASSES", "PASSED", "PASSING", "PASSENGER", "PASSENGERS",
        // Words containing "s*x" patterns
        "SUSSEX",
        // Scunthorpe problem words
        "SCUNTHORPE",
        // Words containing "h*ll" patterns
        "SHELL", "SHELLS", "SHELLED", "SHELLING", "SHELLFISH",
        "HELLO", "HELLOS",
        // Words containing "d*mn" patterns
        "CONDEMN", "CONDEMNS", "CONDEMNED", "CONDEMNING", "CONDEMNATION",
        // Words containing "b*tt" patterns
        "BUTTON", "BUTTONS", "BUTTONED", "BUTTONING",
        "BUTTER", "BUTTERS", "BUTTERED", "BUTTERING", "BUTTERY",
        "BUTTERFLY", "BUTTERFLIES", "BUTTERCUP", "BUTTERCUPS",
        "REBUTTAL", "REBUTTALS",
        // No idea why these got flagged
        "DINING", "DINNER", "DINE", "DINES", "DINED", "DINING",
        //
        "DIRTY"

    }.ToFrozenSet();

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
            return "Word is not in the dictionary (negative words like anti- un- non-, -less are not allowed). If you think this word was blocked incorrectly, discuss in the community subreddit!"; // Not a real word
        }

        // Step 2: Safety Check (Local ML)
        // Skip profanity check if word is in the whitelist (common false positives)
        if (_profanityWhitelist.Contains(word))
        {
            return null; // Whitelisted word, bypass profanity check
        }

        // Returns true if it looks like profanity (e.g. "sh!t")
        if (_profanityDetector.IsProfane(input))
        {
            return "No profanities please. If you think this word was blocked incorrectly, discuss in the community subreddit!"; // Valid Scrabble word, but blocked by safety filter
        }

        return null;
    }
}
}