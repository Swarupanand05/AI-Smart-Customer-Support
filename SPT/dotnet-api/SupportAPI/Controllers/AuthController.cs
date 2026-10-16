using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SupportAPI.Data;
using SupportAPI.Helpers;
using SupportAPI.Models;
using SupportAPI.Services;

namespace SupportAPI.Controllers
{
    // ─── Request DTOs ──────────────────────────────────────────────────────────
    public class LoginRequest
    {
        public string Email    { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string Email    { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class GoogleAuthRequest
    {
        /// <summary>The Google ID token returned by @react-oauth/google on the frontend.</summary>
        public string Credential { get; set; } = string.Empty;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // AuthController
    // POST /api/auth/login    — email + password login (High-speed Async + AsNoTracking)
    // POST /api/auth/register — new account registration (High-speed Async + Auto-Login)
    // POST /api/auth/google   — Google OAuth sign-in / auto-register (Async)
    // POST /api/auth/logout   — stateless logout (clears client-side token)
    // ──────────────────────────────────────────────────────────────────────────
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext   _context;
        private readonly JwtService     _jwtService;
        private readonly IConfiguration _config;

        public AuthController(AppDbContext context, JwtService jwtService, IConfiguration config)
        {
            _context    = context;
            _jwtService = jwtService;
            _config     = config;
        }

        // ─── Fixed Admin Credentials (hardcoded — no DB row needed) ────────────
        private const string AdminEmail    = "admin@support.com";
        private const string AdminPassword = "Admin@123";
        private const string AdminName     = "System Admin";

        // ── POST /api/auth/login (Optimized Non-blocking Async Query) ───────────
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest(new { message = "Email and password are required." });

            var cleanEmail    = req.Email.Trim();
            var cleanPassword = req.Password.Trim();

            // 1. Admin shortcut (Case-insensitive email check)
            if (string.Equals(cleanEmail, AdminEmail, StringComparison.OrdinalIgnoreCase) && cleanPassword == AdminPassword)
            {
                var adminUser = new User
                {
                    UserId   = 0,
                    FullName = AdminName,
                    Email    = AdminEmail,
                    Role     = "Admin"
                };
                return Ok(new
                {
                    message  = "Admin login successful",
                    token    = _jwtService.GenerateToken(adminUser),
                    role     = "Admin",
                    fullName = AdminName,
                    email    = AdminEmail,
                    picture  = (string?)null
                });
            }

            // 2. Regular user (Async + AsNoTracking + Case Insensitive Search)
            try
            {
                var hashed = PasswordHasher.Hash(cleanPassword);
                var user   = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
                    _context.Users.AsNoTracking(),
                    u => u.Email.ToLower() == cleanEmail.ToLower() && u.Password == hashed
                );

                if (user == null)
                    return Unauthorized(new { message = "Invalid email or password." });

                return Ok(new
                {
                    message  = "Login successful",
                    token    = _jwtService.GenerateToken(user),
                    role     = user.Role,
                    fullName = user.FullName,
                    email    = user.Email,
                    picture  = user.Picture
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Auth] Login Database Error: {ex.Message}");
                return StatusCode(500, new { message = "Database error. Please verify LocalDB/SQL Server is running." });
            }
        }

        // ── POST /api/auth/register (Optimized Non-blocking Async + Instant Auto-Login) ──
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest(new { message = "Email and password are required." });

            var cleanEmail    = req.Email.Trim();
            var cleanPassword = req.Password.Trim();
            var cleanName     = (req.FullName ?? "").Trim();

            try
            {
                // Fast Async Check using EntityFrameworkQueryableExtensions
                var exists = await EntityFrameworkQueryableExtensions.AnyAsync(
                    _context.Users,
                    u => u.Email.ToLower() == cleanEmail.ToLower()
                );

                if (exists)
                    return BadRequest(new { message = "User with this email already exists." });

                var user = new User
                {
                    FullName  = string.IsNullOrWhiteSpace(cleanName) ? cleanEmail.Split('@')[0] : cleanName,
                    Email     = cleanEmail,
                    Password  = PasswordHasher.Hash(cleanPassword),
                    Role      = "User",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Auto-generate JWT token for instant auto-login (zero latency redirect)
                var token = _jwtService.GenerateToken(user);

                return Ok(new
                {
                    message  = "User registered successfully",
                    token    = token,
                    role     = user.Role,
                    fullName = user.FullName,
                    email    = user.Email,
                    picture  = user.Picture
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Auth] Register Database Error: {ex.Message}");
                return StatusCode(500, new { message = "Database error. Please verify LocalDB/SQL Server is running." });
            }
        }

        // ── POST /api/auth/google ───────────────────────────────────────────────
        // Validates Google ID token → finds or creates user → returns our JWT
        [HttpPost("google")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleAuthRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Credential))
                return BadRequest(new { message = "Google credential is required." });

            try
            {
                var googleClientId = _config["Google:ClientId"];

                // Verify the token with Google's public keys
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { googleClientId }
                };

                GoogleJsonWebSignature.Payload payload =
                    await GoogleJsonWebSignature.ValidateAsync(req.Credential, settings);

                var googleEmail   = payload.Email;
                var googleName    = payload.Name ?? payload.Email;
                var googlePicture = payload.Picture;

                // Find or create user (Async search)
                var user = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
                    _context.Users,
                    u => u.Email == googleEmail
                );

                if (user == null)
                {
                    // Auto-register new Google user
                    user = new User
                    {
                        FullName     = googleName,
                        Email        = googleEmail,
                        Password     = null,           // No password for Google users
                        Picture      = googlePicture,
                        IsGoogleUser = true,
                        Role         = "User",
                        CreatedAt    = DateTime.UtcNow
                    };
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"[Auth] New Google user registered: {googleEmail}");
                }
                else
                {
                    // Update profile picture if changed
                    if (user.Picture != googlePicture || !user.IsGoogleUser)
                    {
                        user.Picture      = googlePicture;
                        user.IsGoogleUser = true;
                        await _context.SaveChangesAsync();
                    }
                    Console.WriteLine($"[Auth] Google login: {googleEmail}");
                }

                return Ok(new
                {
                    message  = "Google login successful",
                    token    = _jwtService.GenerateToken(user),
                    role     = user.Role,
                    fullName = user.FullName,
                    email    = user.Email,
                    picture  = user.Picture
                });
            }
            catch (InvalidJwtException ex)
            {
                Console.WriteLine($"[Auth] Invalid Google token: {ex.Message}");
                return Unauthorized(new { message = "Invalid Google token. Please try again." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Auth] Google auth error: {ex.Message}");
                return StatusCode(500, new { message = "Google authentication failed. Please try again." });
            }
        }

        // ── POST /api/auth/logout ───────────────────────────────────────────────
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            return Ok(new { message = "Logged out successfully" });
        }
    }
}
