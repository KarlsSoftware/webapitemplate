using Microsoft.EntityFrameworkCore;
using WebAPI.Entities;

namespace WebAPI
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Laptop> Laptops { get; set; }
    }
}