using Kantar.Entities;
using Microsoft.EntityFrameworkCore;
using System;

namespace Kantar.DAL
{
    public class KantarDbContext:DbContext
    {
        public KantarDbContext(DbContextOptions<KantarDbContext> options) : base(options) { }
        public DbSet<ProductKantar> Products { get; set; }
        public DbSet<UnitPrice> UnitPrice { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            
        }

    }
}
