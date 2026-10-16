// Import package for JWT (JSON Web Token) authentication
using Microsoft.AspNetCore.Authentication.JwtBearer;

// Import Entity Framework Core for database operations
using Microsoft.EntityFrameworkCore;

// Import security tools for token validation
using Microsoft.IdentityModel.Tokens;

// Import project database context
using SupportAPI.Data;

// Import custom services (JWT, MongoDB, AI, ML)
using SupportAPI.Services;

// Import encoding functions (used for secret key conversion)
using System.Text;

// Create builder object to configure application services and middleware
var builder = WebApplication.CreateBuilder(args);


// ─────────────────────────────────────────────────────────
// CORS (Cross-Origin Resource Sharing)
// Allows frontend (React) to access backend API
// ─────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    // Create policy named "AllowReactApp"
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy

        // Allow requests only from React frontend
        .WithOrigins("http://localhost:3000")

        // Allow all request headers
        .AllowAnyHeader()

        // Allow all HTTP methods (GET, POST, PUT, DELETE)
        .AllowAnyMethod();
    });
});


// ─────────────────────────────────────────────────────────
// Database Configuration (SQL Server + Entity Framework Core)
// ─────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>

    // Read connection string from appsettings.json
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);


// ─────────────────────────────────────────────────────────
// JWT Authentication Configuration
// Used to secure APIs
// ─────────────────────────────────────────────────────────

// Register JwtService for generating tokens
builder.Services.AddScoped<JwtService>();

// Enable authentication using JWT Bearer scheme
builder.Services.AddAuthentication(
    JwtBearerDefaults.AuthenticationScheme
)

// Configure JWT settings
.AddJwtBearer(options =>
{
    options.TokenValidationParameters =
        new TokenValidationParameters
        {
            // Skip checking issuer
            ValidateIssuer = false,

            // Skip checking audience
            ValidateAudience = false,

            // Check token expiration time
            ValidateLifetime = true,

            // Verify secret signing key
            ValidateIssuerSigningKey = true,

            // Convert secret key into security key
            IssuerSigningKey =
                new SymmetricSecurityKey(

                    // Read secret key from appsettings.json
                    Encoding.UTF8.GetBytes(
                        builder.Configuration["Jwt:Key"]!
                    )
                )
        };
});


// ─────────────────────────────────────────────────────────
// Register Application Services
// ─────────────────────────────────────────────────────────

// Singleton → one object for entire application lifetime
builder.Services.AddSingleton<MongoService>();
builder.Services.AddSingleton<EmailService>();

// Register named HttpClient for Groq API calls (with User-Agent to avoid Cloudflare 1010 block)
builder.Services.AddHttpClient("GroqClient", client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.Add("User-Agent", "SupportAPI/1.0");
});

// Register named HttpClient for Gemini API failover calls
builder.Services.AddHttpClient("GeminiClient", client =>
{
    client.Timeout = TimeSpan.FromSeconds(20);
    client.DefaultRequestHeaders.Add("User-Agent", "SupportAPI/1.0");
});

// AIService uses IHttpClientFactory → must be Scoped (not Singleton)
builder.Services.AddScoped<AIService>();

// Enable API Controllers
builder.Services.AddControllers();

// Enable endpoint metadata (used by OpenAPI)
builder.Services.AddEndpointsApiExplorer();

// Enable built-in .NET 10 OpenAPI documentation
builder.Services.AddOpenApi();

// Register HttpClient for ML service API calls
builder.Services.AddHttpClient<MLService>();


// Build application
var app = builder.Build();

// ── Ensure Database & Tables Are Created Automatically on Startup ──
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
        Console.WriteLine("✅ [Database] SQL Server database and tables ensured successfully.");

        var mongo = scope.ServiceProvider.GetRequiredService<MongoService>();
        await mongo.CreateIndexesAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ [Database] Startup database check warning: {ex.Message}");
    }
}


// ─────────────────────────────────────────────────────────
// Middleware Pipeline
// Controls request flow
// ─────────────────────────────────────────────────────────

// Enable OpenAPI endpoint only in development mode
if (app.Environment.IsDevelopment())
{
    // Serves OpenAPI JSON at /openapi/v1.json
    app.MapOpenApi();
}


// Enable CORS
// Must come BEFORE authentication
app.UseCors("AllowReactApp");


// HTTPS disabled because frontend uses HTTP localhost
// app.UseHttpsRedirection();


// Authenticate JWT token
app.UseAuthentication();


// Check authorization rules
app.UseAuthorization();


// Map controller routes
app.MapControllers();


// Start application
app.Run();