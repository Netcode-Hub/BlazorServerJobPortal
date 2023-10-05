using BlazorServerJobPortal.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace BlazorServerJobPortal.Data.Configuration
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        public DbSet<Registration> Registrations { get; set; }
        public DbSet<PostJob> Jobs { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
    }

}
