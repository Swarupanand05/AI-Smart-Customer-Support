using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SupportAPI.Data;
using SupportAPI.Helpers;
using SupportAPI.Models;
using SupportAPI.Services;
using System.Linq;

namespace SupportAPI.Controllers
{
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

        // LOGIN API
        [HttpPost("login")]
        public IActionResult Login(string email, string password)
        {
            var hashed = PasswordHasher.Hash(password);

            var user = _context.Users
                .FirstOrDefault(u => u.Email == email && u.Password == hashed);

            if (user == null)
                return Unauthorized("Invalid credentials");

            var token = _jwtService.GenerateToken(user);

            return Ok(new
            {
                message = "Login successful",
                token = token,
                role = user.Role
            });
        }

        // REGISTER API
        [HttpPost("register")]
        public IActionResult Register(User user)
        {
            if (_context.Users.Any(u => u.Email == user.Email))
                return BadRequest("User already exists");

            user.Password = PasswordHasher.Hash(user.Password);
            user.Role = "User";

            _context.Users.Add(user);
            _context.SaveChanges();

            return Ok("User Registered Successfully");
        }

        // LOGOUT (basic)
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            // In API, logout is handled on frontend (token remove)
            return Ok("Logged out successfully");
        }
    }
}