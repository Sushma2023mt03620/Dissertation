using Microsoft.EntityFrameworkCore;
using ProductionLineService.Models;

namespace ProductionLineService.Data
{
    public class ProductionDbContext : DbContext
    {
        public ProductionDbContext(DbContextOptions<ProductionDbContext> options)
            : base(options)
        {
        }

        public DbSet<ProductionJob> ProductionJobs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProductionJob>()
                .HasIndex(p => p.JobNumber)
                .IsUnique();

            modelBuilder.Entity<ProductionJob>()
                .Property(p => p.JobNumber)
                .IsRequired()
                .HasMaxLength(50);
        }
    }
}