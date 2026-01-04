using OddOneOut.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;
using System.Security.Claims;
using Microsoft.AspNetCore.HttpOverrides; // Needed for the "me" endpoint
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
builder.Services.AddAuthentication()
    .AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

    // 1. CHANGE THE CALLBACK PATH
    // This aligns the cookie path with the proxy path
    options.CallbackPath = "/api/signin-google";

    // 2. FIX THE CORRELATION COOKIE (Crucial for localhost)
    options.CorrelationCookie.SameSite = SameSiteMode.Unspecified;
    options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.SignInScheme = IdentityConstants.ExternalScheme;
});
// 1. Relax the "Application" cookie (Guest & Local logins)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.SameSite = SameSiteMode.Unspecified;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// 2. Relax the "External" cookie (The one getting lost right now!)
builder.Services.ConfigureExternalCookie(options =>
{
    options.Cookie.SameSite = SameSiteMode.Unspecified;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();
app.UseForwardedHeaders();
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? new[] { "http://localhost:5173" }; // Fallback

app.UseCors(policy => policy
    .WithOrigins(allowedOrigins) // <--- Accepts the array
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowCredentials()); // Required for YOUR app's cookies

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
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        db.Database.Migrate();
        DataSeeder.SeedWordCards(db, env); // Uncomment when you have your seeder ready
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred creating the DB.");
    }
}
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");
app.UseRouting();
// CRITICAL for Container Apps / Docker

app.UseAuthentication();
app.UseAuthorization();


app.MapControllers();

app.Run();
