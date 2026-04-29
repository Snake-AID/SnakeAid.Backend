using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SnakeAid.Core.Domains;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Implements;
using SnakeAid.Service.Implements;

namespace SnakeAid.Tests.Unit;

public class SymptomConfigServiceTests
{
    [Fact]
    public async Task GetAllSymptomConfigAsync_ShouldOrderByDisplayOrderThenId()
    {
        await using var db = CreateDbContext();
        db.SymptomConfigs.AddRange(
            CreateConfig(id: 30, displayOrder: 1, name: "Alpha"),
            CreateConfig(id: 10, displayOrder: 1, name: "Zulu"),
            CreateConfig(id: 20, displayOrder: 1, name: "Middle"),
            CreateConfig(id: 40, displayOrder: 2, name: "Before By Name"));
        await db.SaveChangesAsync();

        var service = new SymptomConfigService(
            new UnitOfWork<SnakeAidDbContext>(db),
            NullLogger<SymptomConfigService>.Instance);

        var result = await service.GetAllSymptomConfigAsync();

        Assert.Equal(new[] { 10, 20, 30, 40 }, result.Select(item => item.Id).ToArray());
    }

    [Fact]
    public async Task DeleteSymptomConfigAsync_ShouldSoftDeleteBySettingIsActiveFalse()
    {
        await using var db = CreateDbContext();
        db.SymptomConfigs.Add(CreateConfig(id: 10, displayOrder: 1, name: "Zulu"));
        await db.SaveChangesAsync();

        var service = new SymptomConfigService(
            new UnitOfWork<SnakeAidDbContext>(db),
            NullLogger<SymptomConfigService>.Instance);

        await service.DeleteSymptomConfigAsync(10);

        var config = await db.SymptomConfigs.SingleAsync(sc => sc.Id == 10);
        Assert.False(config.IsActive);
    }

    [Fact]
    public async Task GetGroupedSymptomConfigsForUIAsync_ShouldExcludeInactiveOptions()
    {
        await using var db = CreateDbContext();
        db.SymptomConfigs.AddRange(
            CreateConfig(id: 10, displayOrder: 1, name: "Active", isActive: true),
            CreateConfig(id: 20, displayOrder: 1, name: "Inactive", isActive: false));
        await db.SaveChangesAsync();

        var service = new SymptomConfigService(
            new UnitOfWork<SnakeAidDbContext>(db),
            NullLogger<SymptomConfigService>.Instance);

        var result = await service.GetGroupedSymptomConfigsForUIAsync();

        var options = Assert.Single(result).Options;
        var option = Assert.Single(options);
        Assert.Equal(10, option.Id);
    }

    [Fact]
    public async Task GetAllSymptomConfigAsync_ShouldIncludeInactiveOptions()
    {
        await using var db = CreateDbContext();
        db.SymptomConfigs.AddRange(
            CreateConfig(id: 10, displayOrder: 1, name: "Active", isActive: true),
            CreateConfig(id: 20, displayOrder: 1, name: "Inactive", isActive: false));
        await db.SaveChangesAsync();

        var service = new SymptomConfigService(
            new UnitOfWork<SnakeAidDbContext>(db),
            NullLogger<SymptomConfigService>.Instance);

        var result = await service.GetAllSymptomConfigAsync();

        Assert.Equal(new[] { 10, 20 }, result.Select(item => item.Id).ToArray());
    }

    private static SnakeAidDbContext CreateDbContext()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<SnakeAidDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new SymptomConfigSqliteDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static SymptomConfig CreateConfig(int id, int displayOrder, string name, bool isActive = true)
    {
        return new SymptomConfig
        {
            Id = id,
            GroupName = "BACKGROUND",
            AttributeKey = "AGE_GROUP",
            AttributeLabel = "Age group",
            DisplayOrder = displayOrder,
            Name = name,
            Category = SymptomCategory.Modifier,
            IsActive = isActive
        };
    }

    private sealed class SymptomConfigSqliteDbContext : SnakeAidDbContext
    {
        public SymptomConfigSqliteDbContext(DbContextOptions<SnakeAidDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var keep = new[]
            {
                typeof(SymptomConfig),
                typeof(VenomType)
            }.ToHashSet();

            var dbSetEntityTypes = typeof(SnakeAidDbContext)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
                .Select(p => p.PropertyType.GetGenericArguments()[0])
                .Distinct();

            foreach (var type in dbSetEntityTypes)
            {
                if (!keep.Contains(type))
                {
                    modelBuilder.Ignore(type);
                }
            }

            modelBuilder.Entity<SymptomConfig>(entity =>
            {
                entity.HasKey(sc => sc.Id);
                entity.Property(sc => sc.Category)
                    .HasConversion<int>()
                    .IsRequired();
                entity.HasOne(sc => sc.VenomType)
                    .WithMany(vt => vt.SymptomConfigs)
                    .HasForeignKey(sc => sc.VenomTypeId)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.Ignore(sc => sc.TimeScoreList);
            });

            modelBuilder.Entity<VenomType>(entity =>
            {
                entity.HasKey(vt => vt.Id);
                entity.Ignore(vt => vt.FirstAidGuideline);
                entity.Ignore(vt => vt.SpeciesVenoms);
            });
        }
    }
}
