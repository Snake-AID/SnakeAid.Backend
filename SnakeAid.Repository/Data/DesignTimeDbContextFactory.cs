using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace SnakeAid.Repository.Data
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<SnakeAidDbContext>
    {
        public SnakeAidDbContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "SnakeAid.Api"))
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<SnakeAidDbContext>();
            var connectionString = configuration.GetConnectionString("SupabaseConnection");

            optionsBuilder.UseNpgsql(connectionString);

            return new SnakeAidDbContext(optionsBuilder.Options);
        }
    }
}