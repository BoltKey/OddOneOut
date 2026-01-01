using OddOneOut.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;
using System.Security.Claims; // Needed for the "me" endpoint
// using Microsoft.AspNetCore.OpenApi; // Uncomment if needed for .WithOpenApi()

var builder = WebApplication.CreateBuilder(args);

// --- 1. Database ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// --- 2. Identity & Auth (THE FIXED PART) ---

// A. Register the Identity API services (Manages Users, Tokens, Cookies)
builder.Services.AddIdentityApiEndpoints<User>()
    .AddEntityFrameworkStores<AppDbContext>();

// B. Configure Password Rules (Moved here from your deleted IdentityCore block)
builder.Services.Configure<IdentityOptions>(options =>
{
    options.User.RequireUniqueEmail = false;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 4;
});

// C. Configure Auth Defaults (Cookies vs Tokens)
builder.Services.Configure<AuthenticationOptions>(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme; // Cookies by default
    options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
});

// --- 3. Other Services ---
builder.Services.AddSingleton<OddOneOut.Services.IWordCheckerService, OddOneOut.Services.WordCheckerService>();
builder.Services.AddAuthorizationBuilder();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

// --- 4. Swagger ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer [jwt]'",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] {}
        }
    });
});

var app = builder.Build();

// --- 5. Pipeline Configuration ---
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        DataSeeder.SeedWordCards(db, env); // Uncomment when you have your seeder ready
    }
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
  using (var scope = app.Services.CreateScope())
{
    // specific name of your DbContext (e.g., AppDbContext, GameContext)
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        context.Database.Migrate(); // This is the command that creates tables
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred creating the DB.");
    }
}
}

//app.UseHttpsRedirection();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");
app.UseRouting();

// --- 6. Endpoints ---


var authGroup = app.MapGroup("/api/auth");

// 2. Login Endpoint (Native Username Support)
authGroup.MapPost("/login", async (SignInManager<User> signInManager, LoginRequest request) =>
{
    // The "false" means "Don't lock the account out on failure"
    // The "true" means "Remember Me" (Persist cookie across restarts)
    var result = await signInManager.PasswordSignInAsync(request.Username, request.Password, isPersistent: true, lockoutOnFailure: false);

    if (result.Succeeded) return Results.Ok();
    return Results.Unauthorized();
});

// 3. Logout Endpoint (Clears the cookie)
authGroup.MapPost("/logout", async (SignInManager<User> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Ok();
});

// 4. Signup Endpoint (Your Custom Logic)
authGroup.MapPost("/signup", async (UserManager<User> userManager, SignInManager<User> signInManager, SignupRequest request) =>
{
    var user = new User
    {
        UserName = request.Username,
        Email = null
    };
    var result = await userManager.CreateAsync(user, request.Password);

    if (!result.Succeeded)
    {
        return Results.ValidationProblem(result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description }));
    }

    // Optional: Auto-login after register
    await signInManager.SignInAsync(user, isPersistent: true);

    return Results.Ok();
});

app.MapControllers();

app.Run();

// 1. DTOs (Clean Contracts)
class LoginRequest { public required string Username { get; set; } public required string Password { get; set; } }
class SignupRequest { public required string Username { get; set; } public required string Password { get; set; } public string? Email { get; set; } }
