using System.Collections.Frozen;

namespace OddOneOut.Services
{
public class WordCheckerService : IWordCheckerService
{
    private readonly FrozenSet<string> _validWords;

    // Blacklist of profane words - simple exact-match approach
    // Only blocks actual inappropriate words, no false positives from substring matching
    private static readonly FrozenSet<string> _profanityBlacklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        // Common profanities and slurs (add more as needed)
        "FUCK", "FUCKER", "FUCKERS", "FUCKING", "FUCKED", "FUCKS",
        "SHIT", "SHITS", "SHITTY", "SHITTING",
        "BITCH", "BITCHES", "BITCHY",
        "ASSHOLE", "ASSHOLES",
        "BASTARD", "BASTARDS",
        "DAMN", "DAMNED", "DAMMIT",
        "CUNT", "CUNTS",
        "DICK", "DICKS",
        "PISS", "PISSED", "PISSING",
        "CRAP", "CRAPPY",
        "WHORE", "WHORES",
        "SLUT", "SLUTS", "SLUTTY",
        // Slurs and hate speech
        "NIGGER", "NIGGERS", "NIGGA", "NIGGAS",
        "FAGGOT", "FAGGOTS", "FAG", "FAGS",
        "RETARD", "RETARDS", "RETARDED",
        "KIKE", "KIKES",
        "SPIC", "SPICS",
        "CHINK", "CHINKS",
        "WETBACK", "WETBACKS",
        "TRANNY", "TRANNIES",
        // Sexual terms
        "PENIS", "PENISES",
        "VAGINA", "VAGINAS",
        "BOOB", "BOOBS", "BOOBIE", "BOOBIES",
        "TITS", "TITTIES", "TITTY",
        "DILDO", "DILDOS",
        "BLOWJOB", "BLOWJOBS",
        "HANDJOB", "HANDJOBS",
        "JIZZ", "CUM", "CUMMING", "CUMSHOT",
        "ORGASM", "ORGASMS",
        "WANK", "WANKER", "WANKERS", "WANKING",
        "MASTURBATE", "MASTURBATING", "MASTURBATION",
        // Crude body parts/functions
        "ANUS", "RECTUM",
        "FART", "FARTS", "FARTING", "FARTED",
        "POOP", "POOPING", "POOPED",
        "PEE", "PEEING", "PEED",
        "BUTTHOLE", "BUTTHOLES",
        // Other offensive
        "NAZI", "NAZIS",
        "HOMO", "HOMOS",
        "DYKE", "DYKES"
    }.ToFrozenSet();

    public WordCheckerService(IWebHostEnvironment env)
    {
        var path = Path.Combine(env.ContentRootPath, "Data", "possibleClues.txt");

        var words = File.ReadAllLines(path)
            .Select(w => w.Trim().ToUpperInvariant());

        _validWords = words.ToFrozenSet();
    }

    public string WordInvalidReason(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "Word is empty.";

        var word = input.ToUpperInvariant();

        // Step 1: Dictionary Check
        if (!_validWords.Contains(word))
        {
            return "Word is not in the dictionary (negative words like anti- un- non-, -less are not allowed). If you think this word was blocked incorrectly, discuss in the community subreddit!";
        }

        // Step 2: Profanity Check (exact match blacklist)
        if (_profanityBlacklist.Contains(word))
        {
            return "No profanities please. If you think this word was blocked incorrectly, discuss in the community subreddit!";
        }

        return null;
    }
}
}