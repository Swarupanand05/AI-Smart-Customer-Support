using System;

namespace SupportAPI.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Password { get; set; }  // Nullable — Google OAuth users have no password
        public string Role { get; set; } = "User";
        public string? Picture { get; set; }   // Google profile picture URL
        public bool IsGoogleUser { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}