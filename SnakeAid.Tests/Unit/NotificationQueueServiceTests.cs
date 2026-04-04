using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Messages.Notifications;
using SnakeAid.Core.Requests.Notification;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Implements;
using SnakeAid.Service.Implements;
using System.Reflection;

namespace SnakeAid.Tests.Unit;

public class NotificationQueueServiceTests
{
    [Fact]
    public async Task PublishAsync_ShouldSwallowTimeout_ForWithdrawalNotifications_AndPersistAppNotification()
    {
        var userId = Guid.NewGuid();

        await using var db = CreateDbContext();
        await SeedAccountAsync(db, userId, AccountRole.User);

        var publishEndpoint = new Mock<MassTransit.IPublishEndpoint>();
        publishEndpoint
            .Setup(endpoint => endpoint.Publish(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()))
            .Returns(async () => await Task.Delay(TimeSpan.FromSeconds(10)));

        var service = new NotificationQueueService(
            publishEndpoint.Object,
            NullLogger<NotificationQueueService>.Instance,
            new UnitOfWork<SnakeAidDbContext>(db));

        var startedAt = DateTime.UtcNow;
        await service.PublishAsync(new NotificationMessage
        {
            UserId = userId,
            Title = "Withdrawal approved",
            Body = "Approved",
            Type = "WITHDRAWAL_APPROVED",
            Data = new Dictionary<string, string> { ["withdrawalId"] = Guid.NewGuid().ToString() }
        });
        var elapsed = DateTime.UtcNow - startedAt;

        Assert.True(elapsed < TimeSpan.FromSeconds(5), $"Expected publish timeout to be swallowed quickly, but elapsed {elapsed}.");
        Assert.Single(db.AppNotifications);
    }

    [Fact]
    public async Task BroadcastAsync_ShouldSwallowTimeout_ForWithdrawalNotifications_AndPersistAppNotifications()
    {
        var admin1 = Guid.NewGuid();
        var admin2 = Guid.NewGuid();

        await using var db = CreateDbContext();
        await SeedAccountAsync(db, admin1, AccountRole.Admin);
        await SeedAccountAsync(db, admin2, AccountRole.Admin);

        var publishEndpoint = new Mock<MassTransit.IPublishEndpoint>();
        publishEndpoint
            .Setup(endpoint => endpoint.Publish(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()))
            .Returns(async () => await Task.Delay(TimeSpan.FromSeconds(10)));

        var service = new NotificationQueueService(
            publishEndpoint.Object,
            NullLogger<NotificationQueueService>.Instance,
            new UnitOfWork<SnakeAidDbContext>(db));

        var startedAt = DateTime.UtcNow;
        var recipientCount = await service.BroadcastAsync(new AdminBroadcastNotificationRequest
        {
            Title = "New withdrawal request",
            Body = "Pending review",
            Type = "WITHDRAWAL_REQUEST_CREATED",
            TargetRoles = new List<AccountRole> { AccountRole.Admin },
            Data = new Dictionary<string, string> { ["withdrawalId"] = Guid.NewGuid().ToString() }
        });
        var elapsed = DateTime.UtcNow - startedAt;

        Assert.Equal(2, recipientCount);
        Assert.True(elapsed < TimeSpan.FromSeconds(5), $"Expected broadcast timeout to be swallowed quickly, but elapsed {elapsed}.");
        Assert.Equal(2, await db.AppNotifications.CountAsync());
    }

    private static SnakeAidDbContext CreateDbContext()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<SnakeAidDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new NotificationQueueSqliteDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static async Task SeedAccountAsync(SnakeAidDbContext db, Guid userId, AccountRole role)
    {
        db.Users.Add(new Account
        {
            Id = userId,
            UserName = $"user.{userId:N}",
            NormalizedUserName = $"USER.{userId:N}",
            Email = $"{userId:N}@example.com",
            NormalizedEmail = $"{userId:N}@example.com".ToUpperInvariant(),
            FullName = $"User {userId:N}",
            Role = role,
            IsActive = true
        });
        await db.SaveChangesAsync();
    }

    private sealed class NotificationQueueSqliteDbContext : SnakeAidDbContext
    {
        public NotificationQueueSqliteDbContext(DbContextOptions<SnakeAidDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var keep = new HashSet<Type>
            {
                typeof(Account),
                typeof(AppNotification)
            };

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

            modelBuilder.Entity<Account>(entity => entity.HasKey(a => a.Id));

            modelBuilder.Entity<AppNotification>(entity =>
            {
                entity.HasKey(n => n.Id);
                entity.HasOne(n => n.User)
                    .WithMany(a => a.AppNotifications)
                    .HasForeignKey(n => n.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
