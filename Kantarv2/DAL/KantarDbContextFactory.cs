using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Kantarv2.DAL
{
    /// <summary>
    /// Design-time factory for EF Core migrations.
    /// This allows EF Core tools to create a DbContext instance at design time.
    /// </summary>
    public class KantarDbContextFactory : IDesignTimeDbContextFactory<KantarDbContext>
    {
        public KantarDbContext CreateDbContext(string[] args)
        {
            // Get environment (default to Development for migrations)
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

            // Build configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
                .Build();

            // Create DbContext options
            var optionsBuilder = new DbContextOptionsBuilder<KantarDbContext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            optionsBuilder.UseNpgsql(connectionString);

            return new KantarDbContext(optionsBuilder.Options);
        }
    }
}
