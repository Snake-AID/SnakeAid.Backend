using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SnakeAid.Core.Domains;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Implements;
using SnakeAid.Service.Implements;

namespace SnakeAid.Tests.Unit;

public class ExpertOnlineStatusServiceTests
{
    [Fact]
    public async Task SetOnlineAsync_ShouldMarkExpertOnline_AndReturnTrue()
    {
        var expertId = Guid.NewGuid();
        await using var db = CreateDbContext();
        await SeedExpertAsync(db, expertId, isOnline: false);

        var service = new ExpertOnlineStatusService(
            new UnitOfWork<SnakeAidDbContext>(db),
            NullLogger<ExpertOnlineStatusService>.Instance);

        var changed = await service.SetOnlineAsync(expertId.ToString());

        var profile = await db.ExpertProfiles.FirstAsync(p => p.AccountId == expertId);
        Assert.True(changed);
        Assert.True(profile.IsOnline);
    }

    [Fact]
    public async Task SetOfflineAsync_ShouldMarkExpertOffline_AndReturnTrue()
    {
        var expertId = Guid.NewGuid();
        await using var db = CreateDbContext();
        await SeedExpertAsync(db, expertId, isOnline: true);

        var service = new ExpertOnlineStatusService(
            new UnitOfWork<SnakeAidDbContext>(db),
            NullLogger<ExpertOnlineStatusService>.Instance);

        var changed = await service.SetOfflineAsync(expertId.ToString());

        var profile = await db.ExpertProfiles.FirstAsync(p => p.AccountId == expertId);
        Assert.True(changed);
        Assert.False(profile.IsOnline);
    }

    [Fact]
    public async Task SetOfflineAsync_ShouldBeIdempotent_WhenExpertAlreadyOffline()
    {
        var expertId = Guid.NewGuid();
        await using var db = CreateDbContext();
        await SeedExpertAsync(db, expertId, isOnline: false);

        var service = new ExpertOnlineStatusService(
            new UnitOfWork<SnakeAidDbContext>(db),
            NullLogger<ExpertOnlineStatusService>.Instance);

        var changed = await service.SetOfflineAsync(expertId.ToString());

        Assert.False(changed);
    }

    private static SnakeAidDbContext CreateDbContext()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<SnakeAidDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new ExpertOnlineStatusSqliteDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static async Task SeedExpertAsync(SnakeAidDbContext db, Guid expertId, bool isOnline)
    {
        db.Set<Account>().Add(new Account
        {
            Id = expertId,
            FullName = "Expert Availability Test",
            UserName = "expert.availability",
            NormalizedUserName = "EXPERT.AVAILABILITY",
            Email = "expert.availability@test.local",
            NormalizedEmail = "EXPERT.AVAILABILITY@TEST.LOCAL",
            IsActive = true,
            Role = AccountRole.Expert
        });

        db.ExpertProfiles.Add(new ExpertProfile
        {
            AccountId = expertId,
            Biography = "Availability test bio",
            ConsultationFee = 150_000m,
            IsOnline = isOnline
        });

        await db.SaveChangesAsync();
    }

    private sealed class ExpertOnlineStatusSqliteDbContext : SnakeAidDbContext
    {
        public ExpertOnlineStatusSqliteDbContext(DbContextOptions<SnakeAidDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var keep = new[]
            {
                typeof(Account),
                typeof(ExpertProfile)
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

            modelBuilder.Entity<Account>(entity =>
            {
                entity.HasKey(a => a.Id);
            });

            modelBuilder.Entity<ExpertProfile>(entity =>
            {
                entity.HasKey(e => e.AccountId);
                entity.HasOne(e => e.Account)
                    .WithOne()
                    .HasForeignKey<ExpertProfile>(e => e.AccountId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
