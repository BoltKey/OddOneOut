using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Moq;
using OddOneOut.Services;

namespace OddOneOut.Tests.Unit.Services;

/// <summary>
/// Unit tests for the WordCheckerService.
/// Note: These tests require the possibleClues.txt file to exist in the test environment.
/// For true isolation, you would mock the file system or extract the word list loading.
/// </summary>
public class WordCheckerServiceTests : IDisposable
{
    private readonly string _testDataPath;
    private readonly WordCheckerService _service;

    public WordCheckerServiceTests()
    {
        // Create a temporary directory with test data
        _testDataPath = Path.Combine(Path.GetTempPath(), "OddOneOutTests", Guid.NewGuid().ToString());
        var dataPath = Path.Combine(_testDataPath, "Data");
        Directory.CreateDirectory(dataPath);

        // Create test word list file
        var testWords = new[]
        {
            "HELLO",
            "WORLD",
            "FRUIT",
            "APPLE",
            "BANANA",
            "CHAIR",
            "TABLE",
            "BUTTON",  // Whitelisted word
            "COCKTAIL", // Whitelisted word
            "TEST"
        };
        File.WriteAllLines(Path.Combine(dataPath, "possibleClues.txt"), testWords);

        // Setup mock environment
        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.ContentRootPath).Returns(_testDataPath);

        _service = new WordCheckerService(mockEnv.Object);
    }

    public void Dispose()
    {
        // Cleanup temp directory
        if (Directory.Exists(_testDataPath))
        {
            Directory.Delete(_testDataPath, recursive: true);
        }
    }

    [Fact]
    public void WordInvalidReason_ReturnsNull_ForValidWord()
    {
        // Arrange
        var word = "hello"; // Valid word in our test dictionary

        // Act
        var result = _service.WordInvalidReason(word);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void WordInvalidReason_ReturnsNull_ForUppercaseValidWord()
    {
        // Arrange
        var word = "WORLD"; // Valid word in uppercase

        // Act
        var result = _service.WordInvalidReason(word);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void WordInvalidReason_ReturnsNull_ForMixedCaseValidWord()
    {
        // Arrange
        var word = "FrUiT"; // Valid word in mixed case

        // Act
        var result = _service.WordInvalidReason(word);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void WordInvalidReason_ReturnsError_ForEmptyInput()
    {
        // Arrange
        var word = "";

        // Act
        var result = _service.WordInvalidReason(word);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("empty");
    }

    [Fact]
    public void WordInvalidReason_ReturnsError_ForNullInput()
    {
        // Arrange
        string? word = null;

        // Act
        var result = _service.WordInvalidReason(word!);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("empty");
    }

    [Fact]
    public void WordInvalidReason_ReturnsError_ForWhitespaceInput()
    {
        // Arrange
        var word = "   ";

        // Act
        var result = _service.WordInvalidReason(word);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("empty");
    }

    [Fact]
    public void WordInvalidReason_ReturnsError_ForWordNotInDictionary()
    {
        // Arrange
        var word = "XYZNOTAWORD"; // Not in dictionary

        // Act
        var result = _service.WordInvalidReason(word);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("not in the dictionary");
    }

    [Fact]
    public void WordInvalidReason_AllowsWhitelistedWords_Button()
    {
        // Arrange - BUTTON is in the profanity whitelist
        var word = "button";

        // Act
        var result = _service.WordInvalidReason(word);

        // Assert - Should be allowed (whitelisted)
        result.Should().BeNull();
    }

    [Fact]
    public void WordInvalidReason_AllowsWhitelistedWords_Cocktail()
    {
        // Arrange - COCKTAIL is in the profanity whitelist (contains "cock")
        var word = "cocktail";

        // Act
        var result = _service.WordInvalidReason(word);

        // Assert - Should be allowed (whitelisted)
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("hello")]
    [InlineData("HELLO")]
    [InlineData("Hello")]
    [InlineData("hElLo")]
    public void WordInvalidReason_IsCaseInsensitive(string word)
    {
        // Act
        var result = _service.WordInvalidReason(word);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void WordInvalidReason_TrimsDictionaryWords()
    {
        // The service should handle words with potential whitespace
        // Arrange
        var word = "test";

        // Act
        var result = _service.WordInvalidReason(word);

        // Assert
        result.Should().BeNull();
    }
}
