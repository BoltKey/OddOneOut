using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OddOneOut.Data;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using OddOneOut.Services;
using Microsoft.AspNetCore.Identity;

namespace OddOneOut.Tests.TestFixtures;

/// <summary>
/// Custom WebApplicationFactory for integration testing.
/// Uses SQLite in-memory database and test authentication.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove all DbContext-related registrations to avoid conflicts
            var dbContextDescriptors = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                           d.ServiceType == typeof(AppDbContext) ||
                           d.ServiceType.FullName?.Contains("EntityFrameworkCore") == true ||
                           d.ServiceType.FullName?.Contains("Npgsql") == true)
                .ToList();

            foreach (var descriptor in dbContextDescriptors)
            {
                services.Remove(descriptor);
            }

            // Also remove generic DbContextOptions
            var genericDbContextOptions = services
                .Where(d => d.ServiceType.IsGenericType && 
                           d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>))
                .ToList();
            
            foreach (var descriptor in genericDbContextOptions)
            {
                services.Remove(descriptor);
            }

            // Remove existing WordCheckerService and replace with mock
            var wordCheckerDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IWordCheckerService));
            if (wordCheckerDescriptor != null)
            {
                services.Remove(wordCheckerDescriptor);
            }
            services.AddSingleton<IWordCheckerService, MockWordCheckerService>();

            // Create and open a persistent SQLite connection
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            // Add SQLite in-memory database
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });

            // Configure authentication to use test scheme as default
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "TestScheme";
                options.DefaultChallengeScheme = "TestScheme";
                options.DefaultScheme = "TestScheme";
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", options => { });

            // Reconfigure authorization to use test scheme
            services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder("TestScheme")
                    .RequireAuthenticatedUser()
                    .Build();
            });

            // Configure GameSettings for tests
            services.Configure<GameSettings>(options =>
            {
                options.InitialGuessRating = 1000;
                options.MaxGuessEnergy = 50;
                options.MaxClueEnergy = 5;
                options.BaseRewardInset = 10;
                options.BasePenaltyInset = 20;
                options.BaseRewardOddOne = 20;
                options.BasePenaltyOddOne = 50;
                options.OddOneOutChance = 0.4f;
                options.GuessEnergyRegenIntervalMinutes = 0.3f;
                options.ClueEnergyRegenIntervalMinutes = 4f;
                options.MinGuessesToGiveClues = 2;
                options.GuessAssignGamesAmt = 100;
                options.RegisterTimeoutMinutes = 0f; // Disable rate limiting in tests
                options.SpamDetectionWindowSeconds = 8;
                options.SpamDetectionMinGuesses = 5;
                options.SpamCooldownMinutes = 2;
            });

            // Configure JWT settings for tests
            services.Configure<JwtSettings>(options =>
            {
                options.Key = "TestSecretKeyThatIsAtLeast32CharactersLong!";
                options.Issuer = "TestIssuer";
                options.Audience = "TestAudience";
                options.ExpiryDays = 30;
            });

            // Configure Google OAuth with dummy values for testing
            services.PostConfigure<Microsoft.AspNetCore.Authentication.Google.GoogleOptions>(
                Microsoft.AspNetCore.Authentication.Google.GoogleDefaults.AuthenticationScheme, 
                options =>
            {
                options.ClientId = "test-client-id";
                options.ClientSecret = "test-client-secret";
            });

            // Build service provider to initialize database
            var sp = services.BuildServiceProvider();

            using (var scope = sp.CreateScope())
            {
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<AppDbContext>();
                var gameSettings = scopedServices.GetRequiredService<IOptions<GameSettings>>().Value;

                // Initialize GameConfig with test settings
                GameConfig.Initialize(gameSettings);

                // Ensure the database is created
                db.Database.EnsureCreated();

                // Seed basic test data
                TestDataSeeder.SeedBasicData(db);
            }
        });
    }

    /// <summary>
    /// Gets a scoped service provider for accessing services in tests.
    /// </summary>
    public IServiceScope CreateScope()
    {
        return Services.CreateScope();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection?.Close();
            _connection?.Dispose();
        }
    }
}

/// <summary>
/// Mock WordCheckerService for integration tests.
/// Allows all words by default, can be configured to reject specific words.
/// </summary>
public class MockWordCheckerService : IWordCheckerService
{
    private readonly HashSet<string> _invalidWords = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _profaneWords = new(StringComparer.OrdinalIgnoreCase);

    public MockWordCheckerService()
    {
        // Add some default profane words for testing
        _profaneWords.Add("badword");
    }

    public string WordInvalidReason(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            return "Word is empty.";
        }

        if (_invalidWords.Contains(word))
        {
            return "Word is not in the dictionary.";
        }

        if (_profaneWords.Contains(word))
        {
            return "No profanities please.";
        }

        return null; // Valid word
    }

    public void AddInvalidWord(string word) => _invalidWords.Add(word);
    public void AddProfaneWord(string word) => _profaneWords.Add(word);
    public void ClearInvalidWords() => _invalidWords.Clear();
}
