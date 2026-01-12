using Kantarv2.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Kantarv2.DAL
{
    public class KantarDbContext : IdentityDbContext<User, IdentityRole<int>, int>
    {
        public KantarDbContext(DbContextOptions<KantarDbContext> options) : base(options)
        {
        }

        // DbSet<User> Users is inherited from IdentityDbContext
        public DbSet<UnitPrice> UnitPrices { get; set; }
        public DbSet<Product> Products { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Call base first to set up Identity tables
            base.OnModelCreating(modelBuilder);

            // Custom DateTime converter for RefreshTokenExpireDate
            var nullableDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
                v => v.HasValue ? (v.Value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v.Value.ToUniversalTime()) : null,
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : null);

            modelBuilder.Entity<User>()
                .Property(e => e.RefreshTokenExpireDate)
                .HasConversion(nullableDateTimeConverter);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }


    }
}
