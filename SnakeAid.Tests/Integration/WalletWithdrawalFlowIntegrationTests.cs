using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Messages.Notifications;
using SnakeAid.Core.Requests.Notification;
using SnakeAid.Core.Responses.Wallet;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Implements;
using SnakeAid.Service.Implements;
using SnakeAid.Service.Interfaces;
using System.Reflection;

namespace SnakeAid.Tests.Integration;

public class WalletWithdrawalFlowIntegrationTests
{
    [Fact]
    public async Task CreateApproveCompleteAsync_ShouldPersistLifecycle_AndKeepSingleDeduction()
    {
        var userId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();

        await using var db = CreateDbContext();
        await SeedAccountAsync(db, userId, AccountRole.User);
        await SeedAccountAsync(db, adminUserId, AccountRole.Admin);
        await SeedWalletAsync(db, userId, 500_000m);

        var notifications = new RecordingNotificationQueueService();
        var service = CreateService(db, notifications);

        var created = await service.CreateWithdrawalRequestAsync(
            userId,
            100_000m,
            "123456789",
            "Vietcombank",
            "Nguyen Van A",
            "970436");
        var approved = await service.ApproveWithdrawalAsync(created.Id, adminUserId, "Approved");
        var completed = await service.CompleteWithdrawalAsync(created.Id, adminUserId, "Transferred");

        Assert.Equal(WalletWithdrawStatus.Completed, completed.Status);
        Assert.Equal(created.Id, approved.Id);
        Assert.False(string.IsNullOrWhiteSpace(approved.VietQrPayload));

        var persisted = await db.WalletWithdraws.SingleAsync(w => w.Id == created.Id);
        Assert.Equal(WalletWithdrawStatus.Completed, persisted.Status);
        Assert.Equal(adminUserId, persisted.ProcessedByAdminId);

        var wallet = await db.Wallets.SingleAsync(w => w.UserId == userId);
        Assert.Equal(400_000m, wallet.Balance);

        var withdrawTransactions = await db.Transactions
            .Where(t => t.ReferenceId == created.Id && t.TransactionType == TransactionType.WithdrawalInitiated)
            .ToListAsync();
        Assert.Single(withdrawTransactions);

        Assert.Single(notifications.BroadcastRequests);
        Assert.Equal(2, notifications.PublishedMessages.Count);
    }

    [Fact]
    public async Task CreateApproveFailAsync_ShouldRefundBalance_ClearQr_AndPersistFinalState()
    {
        var userId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();

        await using var db = CreateDbContext();
        await SeedAccountAsync(db, userId, AccountRole.User);
        await SeedAccountAsync(db, adminUserId, AccountRole.Admin);
        await SeedWalletAsync(db, userId, 500_000m);

        var notifications = new RecordingNotificationQueueService();
        var service = CreateService(db, notifications);

        var created = await service.CreateWithdrawalRequestAsync(
            userId,
            100_000m,
            "123456789",
            "Vietcombank",
            "Nguyen Van A",
            "970436");
        await service.ApproveWithdrawalAsync(created.Id, adminUserId, "Approved");
        var failed = await service.FailWithdrawalAsync(created.Id, adminUserId, "Bank timeout", "Retry manually");

        Assert.Equal(WalletWithdrawStatus.Failed, failed.Status);

        var persisted = await db.WalletWithdraws.SingleAsync(w => w.Id == created.Id);
        Assert.Equal(WalletWithdrawStatus.Failed, persisted.Status);
        Assert.Null(persisted.VietQrPayload);
        Assert.Null(persisted.VietQrImageBase64);
        Assert.Equal("Bank timeout", persisted.RejectionReason);

        var wallet = await db.Wallets.SingleAsync(w => w.UserId == userId);
        Assert.Equal(500_000m, wallet.Balance);

        var adjustment = await db.Transactions.SingleAsync(t =>
            t.ReferenceId == created.Id &&
            t.TransactionType == TransactionType.WithdrawalRefund);
        Assert.Equal(100_000m, adjustment.Amount);

        var initiated = await db.Transactions.SingleAsync(t =>
            t.ReferenceId == created.Id &&
            t.TransactionType == TransactionType.WithdrawalInitiated);
        Assert.Equal(100_000m, initiated.Amount);

        Assert.Single(notifications.BroadcastRequests);
        Assert.Equal(2, notifications.PublishedMessages.Count);
    }

    [Fact]
    public async Task CreateRejectAsync_ShouldRefundBalance_AndPersistRejectedState()
    {
        var userId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();

        await using var db = CreateDbContext();
        await SeedAccountAsync(db, userId, AccountRole.User);
        await SeedAccountAsync(db, adminUserId, AccountRole.Admin);
        await SeedWalletAsync(db, userId, 500_000m);

        var notifications = new RecordingNotificationQueueService();
        var service = CreateService(db, notifications);

        var created = await service.CreateWithdrawalRequestAsync(
            userId,
            100_000m,
            "123456789",
            "Vietcombank",
            "Nguyen Van A",
            "970436");
        var rejected = await service.RejectWithdrawalAsync(created.Id, adminUserId, "Bank account invalid", "Name mismatch");

        Assert.Equal(WalletWithdrawStatus.Rejected, rejected.Status);

        var persisted = await db.WalletWithdraws.SingleAsync(w => w.Id == created.Id);
        Assert.Equal(WalletWithdrawStatus.Rejected, persisted.Status);
        Assert.Equal("Bank account invalid", persisted.RejectionReason);

        var wallet = await db.Wallets.SingleAsync(w => w.UserId == userId);
        Assert.Equal(500_000m, wallet.Balance);

        var transactions = await db.Transactions
            .Where(t => t.ReferenceId == created.Id)
            .OrderBy(t => t.TransactionType)
            .ToListAsync();
        Assert.Equal(2, transactions.Count);
        Assert.Contains(transactions, t => t.TransactionType == TransactionType.WithdrawalInitiated);
        Assert.Contains(transactions, t => t.TransactionType == TransactionType.WithdrawalRefund);

        Assert.Single(notifications.BroadcastRequests);
        Assert.Single(notifications.PublishedMessages);
    }

    private static WalletWithdrawService CreateService(
        SnakeAidDbContext db,
        RecordingNotificationQueueService notifications)
    {
        return new WalletWithdrawService(
            new UnitOfWork<SnakeAidDbContext>(db),
            new FakeWalletService(db),
            new VietQrAdapter(),
            notifications,
            NullLogger<WalletWithdrawService>.Instance);
    }

    private static SnakeAidDbContext CreateDbContext()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<SnakeAidDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new WalletWithdrawalSqliteDbContext(options);
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
            NormalizedEmail = $"{userId:N}@EXAMPLE.COM",
            FullName = $"User {userId:N}",
            Role = role,
            IsActive = true
        });
        await db.SaveChangesAsync();
    }

    private static async Task SeedWalletAsync(SnakeAidDbContext db, Guid userId, decimal balance)
    {
        db.Wallets.Add(new Wallet
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Balance = balance
        });
        await db.SaveChangesAsync();
    }

    private sealed class FakeWalletService(SnakeAidDbContext db) : IWalletService
    {
        public async Task<WalletResponse> GetWalletByUserIdAsync(Guid userId)
        {
            var wallet = await db.Wallets.SingleAsync(w => w.UserId == userId);
            return new WalletResponse
            {
                Id = wallet.Id,
                UserId = wallet.UserId,
                Balance = wallet.Balance,
                CreatedAt = wallet.CreatedAt,
                UpdatedAt = wallet.UpdatedAt
            };
        }
    }

    private sealed class RecordingNotificationQueueService : INotificationQueueService
    {
        public List<NotificationMessage> PublishedMessages { get; } = [];
        public List<AdminBroadcastNotificationRequest> BroadcastRequests { get; } = [];

        public Task PublishAsync(NotificationMessage message, CancellationToken cancellationToken = default)
        {
            PublishedMessages.Add(message);
            return Task.CompletedTask;
        }

        public Task PublishBulkAsync(
            IEnumerable<NotificationMessage> messages,
            IEnumerable<AppNotification> appNotifications,
            CancellationToken cancellationToken = default)
        {
            PublishedMessages.AddRange(messages);
            return Task.CompletedTask;
        }

        public Task<int> BroadcastAsync(AdminBroadcastNotificationRequest request, CancellationToken cancellationToken = default)
        {
            BroadcastRequests.Add(request);
            return Task.FromResult(0);
        }
    }

    private sealed class WalletWithdrawalSqliteDbContext : SnakeAidDbContext
    {
        public WalletWithdrawalSqliteDbContext(DbContextOptions<SnakeAidDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var keep = new HashSet<Type>
            {
                typeof(Account),
                typeof(Wallet),
                typeof(WalletWithdraw),
                typeof(Transaction)
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

            modelBuilder.Entity<Wallet>(entity =>
            {
                entity.HasKey(w => w.Id);
                entity.HasOne(w => w.Account)
                    .WithMany()
                    .HasForeignKey(w => w.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<WalletWithdraw>(entity =>
            {
                entity.HasKey(w => w.Id);
                entity.HasOne(w => w.User)
                    .WithMany()
                    .HasForeignKey(w => w.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(w => w.Wallet)
                    .WithMany()
                    .HasForeignKey(w => w.WalletId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(w => w.ProcessedByAdmin)
                    .WithMany()
                    .HasForeignKey(w => w.ProcessedByAdminId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.HasOne(t => t.User)
                    .WithMany()
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
