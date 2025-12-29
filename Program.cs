using OddOneOut.Data; // <--- ADD THIS
using Microsoft.EntityFrameworkCore; // <--- ADD THIS
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
//builder.Services.AddOpenApi();

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // This prevents the "Cycle Detected" error
        // It simply ignores the "parent" reference if it's already serializing the child
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddEndpointsApiExplorer(); // (Might already be there)
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer [jwt]'",
        Name = "Authorization",
        Type = SecuritySchemeType.Http, // Note: Use Http for simple Bearer
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));





// 1. Add authentication and authorization
builder.Services.AddAuthentication().AddBearerToken(IdentityConstants.BearerScheme);
builder.Services.AddAuthorizationBuilder();

// 2. Add Identity Core and Entity Framework stores
builder.Services.AddIdentityCore<User>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequireDigit = false;           // No numbers required
    options.Password.RequireLowercase = false;       // No lowercase required
    options.Password.RequireUppercase = false;       // No uppercase required
    options.Password.RequireNonAlphanumeric = false; // No special chars (!@#) required
    options.Password.RequiredLength = 4;             // Min length (default is 6)
})
    .AddEntityFrameworkStores<AppDbContext>()
    .AddApiEndpoints(); // <--- This does the magic

// 3. Map the endpoints
var app = builder.Build();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        DataSeeder.SeedWordCards(db);
    }
    //app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.MapIdentityApi<User>(); // <--- Exposes /register, /login, etc.

app.UseHttpsRedirection();



app.MapControllers();

app.Run();
