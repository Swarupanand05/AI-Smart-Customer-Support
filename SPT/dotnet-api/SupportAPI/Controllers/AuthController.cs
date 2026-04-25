using Microsoft.AspNetCore.Mvc;
using SupportAPI.Data;
using SupportAPI.Helpers;
using SupportAPI.Models;
using SupportAPI.Services;

namespace SupportAPI.Controllers
{
    // ─── DTOs ──────────────────────────────────────────────────────────────────
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly JwtService _jwtService;

        public AuthController(AppDbContext context, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        // ─── Fixed Admin Credentials ────────────────────────────────────────────
        private const string AdminEmail    = "admin@support.com";
        private const string AdminPassword = "Admin@123";
        private const string AdminName     = "System Admin";

        // LOGIN API — accepts JSON body
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest req)
        {
            // ── 1. Check hardcoded admin credentials first ───────────────────
            if (req.Email == AdminEmail && req.Password == AdminPassword)
            {
                var adminUser = new User
                {
                    UserId   = 0,
                    FullName = AdminName,
                    Email    = AdminEmail,
                    Role     = "Admin"
                };
                var adminToken = _jwtService.GenerateToken(adminUser);

                return Ok(new
                {
                    message  = "Admin login successful",
                    token    = adminToken,
                    role     = "Admin",
                    fullName = AdminName,
                    email    = AdminEmail
                });
            }

            // ── 2. Regular user — check database ────────────────────────────
            var hashed = PasswordHasher.Hash(req.Password);
            var user = _context.Users
                .FirstOrDefault(u => u.Email == req.Email && u.Password == hashed);

            if (user == null)
                return Unauthorized(new { message = "Invalid email or password." });

            var token = _jwtService.GenerateToken(user);

            return Ok(new
            {
                message  = "Login successful",
                token,
                role     = user.Role,
                fullName = user.FullName,
                email    = user.Email
            });
        }

        // REGISTER API — accepts JSON body
        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequest req)
        {
            if (_context.Users.Any(u => u.Email == req.Email))
                return BadRequest(new { message = "User already exists" });

            var user = new User
            {
                FullName = req.FullName,
                Email = req.Email,
                Password = PasswordHasher.Hash(req.Password),
                Role = "User",
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            return Ok(new { message = "User registered successfully" });
        }

        // LOGOUT
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            return Ok(new { message = "Logged out successfully" });
        }
    }
}
