using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OddOneOut.Data;
using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;

[ApiController]
[Route("api/[controller]")] // Routes will be /api/user/login, /api/user/me, etc.
public class UserController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly JwtSettings _jwtSettings;

    public UserController(
        AppDbContext context,
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IConfiguration configuration,
        IOptions<JwtSettings> jwtSettings)
    {
        _context = context;
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _jwtSettings = jwtSettings.Value;
    }
    
    // Helper method to generate JWT token for a user
    private string GenerateJwtToken(User user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName ?? ""),
            new Claim(ClaimTypes.Email, user.Email ?? ""),
            new Claim("DisplayName", user.DisplayName ?? ""),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(_jwtSettings.ExpiryDays),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // ==========================================
    // 1. GET CURRENT USER INFO
    // ==========================================
    [HttpGet("me"), Authorize]
    public async Task<IActionResult> GetMe()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        // Load the full user entity so we can access computed properties
        var user = await _context.Users
            .Where(u => u.Id == userId)
            .FirstOrDefaultAsync();
            
        if (user == null) return NotFound("User profile not found.");

        // Decay rating for inactive users when they check their profile
        user.decayRating();
        await _context.SaveChangesAsync();

        // Calculate ranks efficiently
        var guessRank = await _context.Users.CountAsync(u => u.GuessRating > user.GuessRating) + 1;
        var clueRank = await _context.Users.CountAsync(u => u.CachedClueRating > user.CachedClueRating) + 1;
        
        // Get guess count
        var guessCount = await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => u.Guesses.Count)
            .FirstOrDefaultAsync();

        var response = new UserProfileDto
        {
            Id = user.Id,
            UserName = user.UserName,
            DisplayName = user.DisplayName,
            Email = user.Email,
            GuessRating = user.GuessRating,
            ClueRating = user.CachedClueRating,
            GuessEnergy = user.GuessEnergy,
            ClueEnergy = user.ClueEnergy,
            MaxGuessEnergy = GameConfig.Current.MaxGuessEnergy,
            MaxClueEnergy = GameConfig.Current.MaxClueEnergy,
            NextGuessRegenTime = user.NextGuessRegenTime,
            NextClueRegenTime = user.NextClueRegenTime,
            GuessRank = guessRank,
            ClueRank = clueRank,
            IsGuest = user.IsGuest,
            canGiveClues = guessCount >= GameConfig.Current.MinGuessesToGiveClues,
            guessesToGiveClues = GameConfig.Current.MinGuessesToGiveClues
        };

        return Ok(response);
    }

    [HttpPost("changeDisplayName"), Authorize]
    public async Task<IActionResult> ChangeDisplayName([FromBody] ChangeDisplayNameRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return NotFound("User profile not found.");

        user.DisplayName = request.NewDisplayName;
        await _context.SaveChangesAsync();

        return Ok("Display name updated successfully.");
    }

    // ==========================================
    // 2. STANDARD AUTH (Login, Logout, Signup)
    // ==========================================
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _signInManager.PasswordSignInAsync(request.Username, request.Password, isPersistent: true, lockoutOnFailure: false);
        if (result.Succeeded) return Ok();
        return Unauthorized();
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Ok();
    }

    [HttpPost("signup")]
    public async Task<IActionResult> Signup([FromBody] SignupRequest request)
    {
        // Check if the user is currently a Guest upgrading their account
        if (User.Identity.IsAuthenticated)
        {
            return await UpgradeGuestToRegistered(request);
        }
        // Optimize: Check in database instead of loading all users
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var recentAccountCount = await _context.Users
            .Where(u => u.SourceIp == ipAddress && 
                   u.CreatedAt > DateTime.UtcNow.AddMinutes(-GameConfig.Current.RegisterTimeoutMinutes))
            .CountAsync();
        
        Console.WriteLine($"Recent accounts from this IP: {recentAccountCount}");
        if (recentAccountCount > 0)
        {
            return BadRequest("You cannot create so many accounts from the same IP. Wait a while please.");
        }

        // Otherwise, create a new User
        var user = new User { UserName = request.Username, Email = null };
        user.DisplayName = request.Username;
        user.SourceIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        user.GuessEnergy = GameConfig.Current.MaxGuessEnergy;
        user.ClueEnergy = GameConfig.Current.MaxClueEnergy;
        user.GuessRating = GameConfig.Current.InitialGuessRating;
        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
             return BadRequest(result.Errors);

        await _signInManager.SignInAsync(user, isPersistent: true);
        return Ok();
    }

    // ==========================================
    // 3. GUEST LOGIC
    // ==========================================
    [HttpPost("create-guest")]
    public async Task<IActionResult> CreateGuest([FromQuery] string? guestUserId = null)
    {
        // If a guest user ID is provided, try to reuse that guest account
        if (!string.IsNullOrEmpty(guestUserId))
        {
            var existingGuest = await _userManager.FindByIdAsync(guestUserId);
            if (existingGuest != null && existingGuest.IsGuest)
            {
                // Found the guest user, sign them in
                await _signInManager.SignInAsync(existingGuest, isPersistent: true);
                return Ok(new { userId = existingGuest.Id });
            }
            // If guest user not found or not a guest, fall through to create new guest
        }

        // Create a new guest user
        var guestId = Guid.NewGuid().ToString().Substring(0, 8);
        // Use your custom 'User' class here
        var guestUser = new User
        {
            UserName = $"guest_{guestId}"
        };
        // Optimize: Check in database instead of loading all users
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var recentAccountCount = await _context.Users
            .Where(u => u.SourceIp == ipAddress && 
                   u.CreatedAt > DateTime.UtcNow.AddMinutes(-GameConfig.Current.RegisterTimeoutMinutes))
            .CountAsync();
        
        Console.WriteLine($"Recent accounts from this IP: {recentAccountCount}");
        if (recentAccountCount > 0)
        {
            return BadRequest("You cannot create so many accounts from the same IP. Wait a while please.");
        }
        guestUser.DisplayName = $"Guest_{guestId}";
        guestUser.IsGuest = true;
        guestUser.SourceIp = HttpContext.Connection.RemoteIpAddress?.ToString();

        guestUser.GuessEnergy = GameConfig.Current.MaxGuessEnergy;
        guestUser.ClueEnergy = GameConfig.Current.MaxClueEnergy;
        guestUser.GuessRating = GameConfig.Current.InitialGuessRating;

        var result = await _userManager.CreateAsync(guestUser);

        if (result.Succeeded)
        {
            await _signInManager.SignInAsync(guestUser, isPersistent: true);
            return Ok(new { userId = guestUser.Id });
        }
        return BadRequest(result.Errors);
    }

    // Helper to upgrade a Guest to a real account
    private async Task<IActionResult> UpgradeGuestToRegistered(SignupRequest request)
    {
        var user = await _userManager.GetUserAsync(User);
        var isGuest = user.IsGuest;

        if (!isGuest) return BadRequest("You are already registered.");

        // 1. Update details
        user.UserName = request.Username;
        user.DisplayName = request.Username;
        // user.Email = request.Email; // Uncomment if your request has email

        // 2. Set Password
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var pwResult = await _userManager.ResetPasswordAsync(user, token, request.Password);
        if (!pwResult.Succeeded) return BadRequest(pwResult.Errors);

        // 3. Remove Guest Claim
        user.IsGuest = false;
        await _userManager.UpdateAsync(user);

        await _signInManager.RefreshSignInAsync(user);
        return Ok("Guest account upgraded!");
    }

    // ==========================================
    // 4. REDDIT/DEVVIT AUTH
    // ==========================================
    
    /// <summary>
    /// Authenticate a Reddit user coming from Devvit/Reddit Games.
    /// This endpoint receives the Reddit user ID from the Devvit context
    /// and creates or finds the corresponding user account.
    /// </summary>
    [HttpPost("reddit-login")]
    public async Task<IActionResult> RedditLogin([FromBody] RedditLoginRequest request)
    {
        if (string.IsNullOrEmpty(request.RedditUserId))
        {
            return BadRequest("Reddit user ID is required.");
        }

        // Validate that it looks like a Reddit T2 ID (e.g., "t2_abc123")
        if (!request.RedditUserId.StartsWith("t2_"))
        {
            return BadRequest("Invalid Reddit user ID format.");
        }

        // Check if user is already authenticated and trying to link accounts
        if (User.Identity.IsAuthenticated)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                // If user already has a Reddit ID linked, check if it matches
                if (!string.IsNullOrEmpty(currentUser.RedditUserId))
                {
                    if (currentUser.RedditUserId == request.RedditUserId)
                    {
                        // Already linked to this Reddit account - just return success
                        return Ok(new { userId = currentUser.Id, linked = true });
                    }
                    else
                    {
                        // Different Reddit account - don't allow re-linking
                        return BadRequest("This account is already linked to a different Reddit account.");
                    }
                }
                
                // Link the Reddit account to the current user (guest upgrade scenario)
                currentUser.RedditUserId = request.RedditUserId;
                if (currentUser.IsGuest)
                {
                    currentUser.IsGuest = false;
                    // Set display name from Reddit username if provided
                    if (!string.IsNullOrEmpty(request.RedditUsername))
                    {
                        currentUser.DisplayName = request.RedditUsername;
                        currentUser.UserName = await GetUniqueUsernameAsync(request.RedditUsername, null);
                    }
                }
                await _userManager.UpdateAsync(currentUser);
                await _signInManager.RefreshSignInAsync(currentUser);
                return Ok(new { userId = currentUser.Id, linked = true });
            }
        }

        // Try to find existing user by Reddit ID
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.RedditUserId == request.RedditUserId);

        if (existingUser != null)
        {
            // Found existing Reddit user - sign them in
            await _signInManager.SignInAsync(existingUser, isPersistent: true);
            return Ok(new { userId = existingUser.Id, isNew = false });
        }

        // Create a new user for this Reddit account
        var redditId = request.RedditUserId.Substring(3); // Remove "t2_" prefix for username
        var username = !string.IsNullOrEmpty(request.RedditUsername) 
            ? await GetUniqueUsernameAsync(request.RedditUsername, null)
            : await GetUniqueUsernameAsync($"Reddit_{redditId}", null);
        
        var newUser = new User
        {
            UserName = username,
            RedditUserId = request.RedditUserId,
            DisplayName = request.RedditUsername ?? $"Reddit_{redditId}",
            IsGuest = false,
            SourceIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
            GuessEnergy = GameConfig.Current.MaxGuessEnergy,
            ClueEnergy = GameConfig.Current.MaxClueEnergy,
            GuessRating = GameConfig.Current.InitialGuessRating
        };

        var createResult = await _userManager.CreateAsync(newUser);

        if (createResult.Succeeded)
        {
            await _signInManager.SignInAsync(newUser, isPersistent: true);
            return Ok(new { userId = newUser.Id, isNew = true });
        }

        return BadRequest(createResult.Errors);
    }

    // ==========================================
    // 4b. ITCH.IO LOGIN
    // ==========================================
    
    /// <summary>
    /// Authenticate an itch.io user.
    /// This endpoint receives the itch.io user ID from the itch.io JavaScript API
    /// and creates or finds the corresponding user account.
    /// </summary>
    [HttpPost("itchio-login")]
    public async Task<IActionResult> ItchioLogin([FromBody] ItchioLoginRequest request)
    {
        if (request.ItchioUserId <= 0)
        {
            return BadRequest("Itch.io user ID is required.");
        }

        var itchioUserIdStr = request.ItchioUserId.ToString();

        // Check if user is already authenticated and trying to link accounts
        if (User.Identity.IsAuthenticated)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                // If user already has an itch.io ID linked, check if it matches
                if (!string.IsNullOrEmpty(currentUser.ItchioUserId))
                {
                    if (currentUser.ItchioUserId == itchioUserIdStr)
                    {
                        // Already linked to this itch.io account - just return success
                        return Ok(new { userId = currentUser.Id, linked = true });
                    }
                    else
                    {
                        // Different itch.io account - don't allow re-linking
                        return BadRequest("This account is already linked to a different itch.io account.");
                    }
                }
                
                // Link the itch.io account to the current user (guest upgrade scenario)
                currentUser.ItchioUserId = itchioUserIdStr;
                if (currentUser.IsGuest)
                {
                    currentUser.IsGuest = false;
                    // Set display name from itch.io username if provided
                    if (!string.IsNullOrEmpty(request.ItchioUsername))
                    {
                        currentUser.DisplayName = request.ItchioUsername;
                        currentUser.UserName = await GetUniqueUsernameAsync(request.ItchioUsername, null);
                    }
                }
                await _userManager.UpdateAsync(currentUser);
                await _signInManager.RefreshSignInAsync(currentUser);
                return Ok(new { userId = currentUser.Id, linked = true });
            }
        }

        // Try to find existing user by itch.io ID
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.ItchioUserId == itchioUserIdStr);

        if (existingUser != null)
        {
            // Found existing itch.io user - sign them in
            await _signInManager.SignInAsync(existingUser, isPersistent: true);
            return Ok(new { userId = existingUser.Id, isNew = false });
        }

        // Create a new user for this itch.io account
        var username = !string.IsNullOrEmpty(request.ItchioUsername) 
            ? await GetUniqueUsernameAsync(request.ItchioUsername, null)
            : await GetUniqueUsernameAsync($"Itchio_{itchioUserIdStr}", null);
        
        var newUser = new User
        {
            UserName = username,
            ItchioUserId = itchioUserIdStr,
            DisplayName = request.ItchioUsername ?? $"Itchio_{itchioUserIdStr}",
            IsGuest = false,
            SourceIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
            GuessEnergy = GameConfig.Current.MaxGuessEnergy,
            ClueEnergy = GameConfig.Current.MaxClueEnergy,
            GuessRating = GameConfig.Current.InitialGuessRating
        };

        var createResult = await _userManager.CreateAsync(newUser);

        if (createResult.Succeeded)
        {
            await _signInManager.SignInAsync(newUser, isPersistent: true);
            return Ok(new { userId = newUser.Id, isNew = true });
        }

        return BadRequest(createResult.Errors);
    }

    // ==========================================
    // 5. GOOGLE AUTH
    // ==========================================

    // Step A: Trigger the redirect to Google
    [HttpGet("login-google")]
    public IActionResult LoginGoogle([FromQuery] bool popup = false)
    {
        // Include popup flag in the callback URL so we know how to respond
        var redirectUrl = Url.Action("GoogleCallback", "user", new { popup }, Request.Scheme);
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(GoogleDefaults.AuthenticationScheme, redirectUrl);
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    // Step B: Handle the return from Google
    [HttpGet("google-callback")]
    public async Task<IActionResult> GoogleCallback([FromQuery] bool popup = false)
    {
        var info = await _signInManager.GetExternalLoginInfoAsync();
        var clientUrl = _configuration["ClientUrl"] ?? "http://localhost:5173";
        
        if (info == null)
        {
            return popup ? PopupResponse("error", "auth_failed") : Redirect($"{clientUrl}/login?error=auth_failed");
        }

        // Grab the Google Name (e.g., "John Doe") and Email
        var googleName = info.Principal.FindFirstValue(ClaimTypes.Name);
        var googleEmail = info.Principal.FindFirstValue(ClaimTypes.Email);

        // 1. Is this a user trying to merge (Guest Upgrade)?
        if (User.Identity.IsAuthenticated)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // Handle "Ghost User" (Cookie exists, but DB record is gone)
            if (currentUser == null)
            {
                await _signInManager.SignOutAsync();
                // Fall through to "New User" logic below...
            }
            else
            {
                var isGuest = currentUser.IsGuest;

                if (isGuest)
                {
                    var addLogin = await _userManager.AddLoginAsync(currentUser, info);
                    if (addLogin.Succeeded)
                    {
                        // UPDATE: Set a friendly public username instead of email
                        currentUser.UserName = await GetUniqueUsernameAsync(googleName, googleEmail);
                        currentUser.Email = googleEmail;
                        currentUser.IsGuest = false;
                        currentUser.DisplayName = googleName ?? currentUser.DisplayName;
                        await _userManager.UpdateAsync(currentUser);


                        // Refresh cookie
                        await _signInManager.SignInAsync(currentUser, isPersistent: true);
                        return popup ? PopupResponse("success", null, currentUser) : Redirect($"{clientUrl}/");
                    }
                }
                return popup ? PopupResponse("success", null, currentUser) : Redirect($"{clientUrl}/");
            }
        }

        // 2. Standard Login (Existing Google User)
        var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: true);
        if (result.Succeeded)
        {
            // For popup mode, we need the user object to generate JWT token
            if (popup)
            {
                var existingUser = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                return PopupResponse("success", null, existingUser);
            }
            return Redirect($"{clientUrl}/");
        }

        // 3. New User Registration
        // UPDATE: Generate friendly username here too
        var newUserName = await GetUniqueUsernameAsync(googleName, googleEmail);
        var newUser = new User { UserName = newUserName, Email = googleEmail };
        newUser.DisplayName = googleName;
        newUser.GuessEnergy = GameConfig.Current.MaxGuessEnergy;
        newUser.ClueEnergy = GameConfig.Current.MaxClueEnergy;
        newUser.GuessRating = GameConfig.Current.InitialGuessRating;

        var createResult = await _userManager.CreateAsync(newUser);

        if (createResult.Succeeded)
        {
            await _userManager.AddLoginAsync(newUser, info);
            await _signInManager.SignInAsync(newUser, isPersistent: true);
            return popup ? PopupResponse("success", null, newUser) : Redirect($"{clientUrl}/");
        }

        return popup ? PopupResponse("error", "unknown") : Redirect($"{clientUrl}/login?error=unknown");
    }
    
    // Helper method to return HTML that posts a message to the opener window and closes itself
    // This is used for popup-based OAuth flow (PWA support)
    // When a user is provided, includes a JWT token for iframe contexts (like itch.io) where cookies don't work
    private ContentResult PopupResponse(string status, string? error = null, User? user = null)
    {
        var messageType = status == "success" ? "google-auth-success" : "google-auth-error";
        var errorJson = error != null ? $", error: '{error}'" : "";
        
        // Generate JWT token if user is provided (for iframe/itch.io support)
        var tokenJson = "";
        if (user != null && status == "success")
        {
            var token = GenerateJwtToken(user);
            tokenJson = $", token: '{token}'";
        }
        
        var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Authentication Complete</title>
</head>
<body>
    <p>Authentication complete. This window will close automatically.</p>
    <script>
        if (window.opener) {{
            window.opener.postMessage({{ type: '{messageType}'{errorJson}{tokenJson} }}, '*');
        }}
        window.close();
        // Fallback: if window.close() doesn't work (some browsers block it),
        // show a message to the user
        setTimeout(function() {{
            document.body.innerHTML = '<p>Authentication complete! You can close this window.</p>';
        }}, 500);
    </script>
</body>
</html>";
        
        return Content(html, "text/html");
    }

    // ==========================================
    // HELPER METHOD (Put this inside UserController)
    // ==========================================
    private async Task<string> GetUniqueUsernameAsync(string name, string email)
    {
        // 1. Determine the "Base" Name
        string baseName = "Player";

        if (!string.IsNullOrWhiteSpace(name))
        {
            // "John Doe" -> "JohnDoe" (Keep only letters and digits)
            baseName = System.Text.RegularExpressions.Regex.Replace(name, "[^a-zA-Z0-9]", "");
        }
        else if (!string.IsNullOrWhiteSpace(email))
        {
            // "john.doe@email.com" -> "johndoe"
            var emailPrefix = email.Split('@')[0];
            baseName = System.Text.RegularExpressions.Regex.Replace(emailPrefix, "[^a-zA-Z0-9]", "");
        }

        // Fallback if regex stripped everything (e.g. user name was "!!!")
        if (string.IsNullOrEmpty(baseName)) baseName = "Player";

        // 2. Check if the clean name is available
        var user = await _userManager.FindByNameAsync(baseName);
        if (user == null)
        {
            return baseName; // It's free! Use "JohnDoe"
        }

        // 3. If taken, loop until we find a free one
        // We use a random number strategy to avoid "JohnDoe1", "JohnDoe2" predictability collisions
        var random = new Random();
        while (true)
        {
            // Generate a candidate: "JohnDoe" + Random Number (100-9999)
            var suffix = random.Next(100, 99999).ToString();
            var candidate = $"{baseName}{suffix}";

            var takenUser = await _userManager.FindByNameAsync(candidate);
            if (takenUser == null)
            {
                return candidate; // Found a free one!
            }
            // If taken, the loop runs again with a new random number
        }
    }
}
public class LoginRequest
{
    public string Username { get; set; }
    public string Password { get; set; }
}

public class SignupRequest
{
    public string Username { get; set; }
    public string Password { get; set; }
}

public class UserProfileDto
{
    public string Id { get; set; }
    public string UserName { get; set; }
    public string DisplayName { get; set; }
    public string Email { get; set; }
    public int GuessRating { get; set; }
    public int GuessRank { get; set; }
    public float ClueRating { get; set; }
    public int ClueRank { get; set; }
    public int GuessEnergy { get; set; }
    public int ClueEnergy { get; set; }
    public int MaxGuessEnergy { get; set; }
    public int MaxClueEnergy { get; set; }
    public bool IsGuest { get; set; }
    public DateTime? NextGuessRegenTime { get; set; }
    public DateTime? NextClueRegenTime { get; set; }
    public bool canGiveClues { get; set; }
    public int guessesToGiveClues { get; set; }
}
public class ChangeDisplayNameRequest
{
    public string NewDisplayName { get; set; }
}

public class RedditLoginRequest
{
    public string RedditUserId { get; set; }
    public string? RedditUsername { get; set; }
}

public class ItchioLoginRequest
{
    public long ItchioUserId { get; set; }
    public string? ItchioUsername { get; set; }
}