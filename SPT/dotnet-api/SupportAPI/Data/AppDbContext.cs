using Microsoft.EntityFrameworkCore;
using SupportAPI.Models;

namespace SupportAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Add index on Email for high-speed O(1) login/registration queries
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();
        }
    }
}