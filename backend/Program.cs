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
    options.User.RequireUniqueEmail = true;
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
        // DataSeeder.SeedWordCards(db); // Uncomment when you have your seeder
    }
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// --- 6. Endpoints ---

// A. Map the Identity API (Login, Register, etc.)
app.MapGroup("/api/auth").MapIdentityApi<User>();

// B. Map the "Get Me" endpoint (Crucial for your React App)
app.MapGet("/api/users/me", async (ClaimsPrincipal user, AppDbContext db) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

    var userData = await db.Users
        .Include(u => u.CurrentGame)
            .ThenInclude(g => g.CardSet)
            .ThenInclude(cs => cs.WordCards)
        .Include(u => u.Guesses)
        .FirstOrDefaultAsync(u => u.Id == userId);

    return userData is null ? Results.NotFound() : Results.Ok(userData);
})
.RequireAuthorization();

app.MapControllers();

app.Run();