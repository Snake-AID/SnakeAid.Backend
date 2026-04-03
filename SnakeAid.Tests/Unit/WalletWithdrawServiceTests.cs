using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Messages.Notifications;
using SnakeAid.Core.Requests.Notification;
using SnakeAid.Core.Responses.Wallet;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Implements;
using SnakeAid.Service.Implements;
using SnakeAid.Service.Interfaces;
using System.Reflection;

namespace SnakeAid.Tests.Unit;

public class WalletWithdrawServiceTests
{
    [Fact]
    public async Task CreateWithdrawalRequestAsync_ShouldCreatePendingWithdrawal_AndAdminBroadcast()
    {
        var userId = Guid.NewGuid();

        await using var db = CreateDbContext();
        await SeedAccountAsync(db, userId, AccountRole.User);
        await SeedWalletAsync(db, userId, 500_000m);

        var notifications = new RecordingNotificationQueueService();
        var service = CreateService(db, notifications);

        var withdrawal = await service.CreateWithdrawalRequestAsync(
            userId,
            100_000m,
            "123456789",
            "Vietcombank",
            "Nguyen Van A",
            "970436");

        Assert.Equal(WalletWithdrawStatus.Pending, withdrawal.Status);
        Assert.Equal(userId, withdrawal.UserId);
        Assert.Equal(100_000m, withdrawal.Amount);

        var wallet = await db.Wallets.FirstAsync(w => w.UserId == userId);
        Assert.Equal(500_000m, wallet.Balance);

        var broadcast = Assert.Single(notifications.BroadcastRequests);
        Assert.Equal("WITHDRAWAL_REQUEST_CREATED", broadcast.Type);
        Assert.Contains(AccountRole.Admin, broadcast.TargetRoles ?? []);
        Assert.Equal(withdrawal.Id.ToString(), broadcast.Data?["withdrawalId"]);
    }

    [Fact]
    public async Task CreateWithdrawalRequestAsync_ShouldThrowValidationException_WhenAmountOutOfRange()
    {
        var userId = Guid.NewGuid();

        await using var db = CreateDbContext();
        await SeedAccountAsync(db, userId, AccountRole.User);
        await SeedWalletAsync(db, userId, 500_000m);

        var service = CreateService(db, new RecordingNotificationQueueService());

        var exception = await Assert.ThrowsAsync<ValidationException>(() => service.CreateWithdrawalRequestAsync(
            userId,
            10_000m,
            "123456789",
            "Vietcombank",
            "Nguyen Van A",
            "970436"));

        Assert.Equal("VALIDATION_ERROR", exception.ErrorCode);
        Assert.Contains("amount", exception.ValidationErrors.Keys);
    }

    [Fact]
    public async Task CancelWithdrawalAsync_ShouldRejectPendingWithdrawal_AndAdminBroadcast()
    {
        var userId = Guid.NewGuid();
        var withdrawalId = Guid.NewGuid();

        await using var db = CreateDbContext();
        var wallet = await SeedWalletWithUserAsync(db, userId, 500_000m);
        db.WalletWithdraws.Add(new WalletWithdraw
        {
            Id = withdrawalId,
            UserId = userId,
            WalletId = wallet.Id,
            Amount = 100_000m,
            BankAccount = "123456789",
            BankName = "Vietcombank",
            AccountHolderName = "Nguyen Van A",
            BankBin = "970436",
            Status = WalletWithdrawStatus.Pending
        });
        await db.SaveChangesAsync();

        var notifications = new RecordingNotificationQueueService();
        var service = CreateService(db, notifications);

        var result = await service.CancelWithdrawalAsync(withdrawalId, userId);

        Assert.Equal(WalletWithdrawStatus.Rejected, result.Status);
        Assert.Equal("Cancelled by user", result.RejectionReason);

        var broadcast = Assert.Single(notifications.BroadcastRequests);
        Assert.Equal("WITHDRAWAL_CANCELLED", broadcast.Type);
        Assert.Equal(withdrawalId.ToString(), broadcast.Data?["withdrawalId"]);
    }

    [Fact]
    public async Task ApproveWithdrawalAsync_ShouldDeductWallet_CreateTransaction_AndUserNotification()
    {
        var userId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var withdrawalId = Guid.NewGuid();

        await using var db = CreateDbContext();
        var wallet = await SeedWalletWithUserAsync(db, userId, 500_000m);
        await SeedAccountAsync(db, adminUserId, AccountRole.Admin);

        db.WalletWithdraws.Add(new WalletWithdraw
        {
            Id = withdrawalId,
            UserId = userId,
            WalletId = wallet.Id,
            Amount = 100_000m,
            BankAccount = "123456789",
            BankName = "Vietcombank",
            AccountHolderName = "Nguyen Van A",
            BankBin = "970436",
            Status = WalletWithdrawStatus.Pending
        });
        await db.SaveChangesAsync();

        var notifications = new RecordingNotificationQueueService();
        var service = CreateService(db, notifications);

        var result = await service.ApproveWithdrawalAsync(withdrawalId, adminUserId, "Approved for payout");

        Assert.Equal(WalletWithdrawStatus.Approved, result.Status);
        Assert.Equal(adminUserId, result.ProcessedByAdminId);
        Assert.False(string.IsNullOrWhiteSpace(result.VietQrPayload));

        var refreshedWallet = await db.Wallets.FirstAsync(w => w.UserId == userId);
        Assert.Equal(400_000m, refreshedWallet.Balance);

        var transaction = await db.Transactions.SingleAsync(t =>
            t.ReferenceId == withdrawalId &&
            t.TransactionType == TransactionType.WalletWithdraw);
        Assert.Equal(100_000m, transaction.Amount);

        var notification = Assert.Single(notifications.PublishedMessages);
        Assert.Equal(userId, notification.UserId);
        Assert.Equal("WITHDRAWAL_APPROVED", notification.Type);
        Assert.Equal(withdrawalId.ToString(), notification.Data?["withdrawalId"]);
    }

    [Fact]
    public async Task FailWithdrawalAsync_ShouldRefundWallet_ClearQr_CreateAdjustment_AndUserNotification()
    {
        var userId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var withdrawalId = Guid.NewGuid();

        await using var db = CreateDbContext();
        var wallet = await SeedWalletWithUserAsync(db, userId, 400_000m);
        await SeedAccountAsync(db, adminUserId, AccountRole.Admin);

        db.WalletWithdraws.Add(new WalletWithdraw
        {
            Id = withdrawalId,
            UserId = userId,
            WalletId = wallet.Id,
            Amount = 100_000m,
            BankAccount = "123456789",
            BankName = "Vietcombank",
            AccountHolderName = "Nguyen Van A",
            BankBin = "970436",
            Status = WalletWithdrawStatus.Approved,
            VietQrPayload = "payload",
            VietQrImageBase64 = "image"
        });
        await db.SaveChangesAsync();

        var notifications = new RecordingNotificationQueueService();
        var service = CreateService(db, notifications);

        var result = await service.FailWithdrawalAsync(withdrawalId, adminUserId, "Bank transfer timeout", "Retry later");

        Assert.Equal(WalletWithdrawStatus.Failed, result.Status);
        Assert.Null(result.VietQrPayload);
        Assert.Null(result.VietQrImageBase64);
        Assert.Equal("Bank transfer timeout", result.RejectionReason);

        var refreshedWallet = await db.Wallets.FirstAsync(w => w.UserId == userId);
        Assert.Equal(500_000m, refreshedWallet.Balance);

        var transaction = await db.Transactions.SingleAsync(t =>
            t.ReferenceId == withdrawalId &&
            t.TransactionType == TransactionType.AdminAdjustment);
        Assert.Equal(100_000m, transaction.Amount);

        var notification = Assert.Single(notifications.PublishedMessages);
        Assert.Equal("WITHDRAWAL_FAILED", notification.Type);
        Assert.Equal("Bank transfer timeout", notification.Data?["reason"]);
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

        var context = new WalletWithdrawSqliteDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static async Task<Wallet> SeedWalletWithUserAsync(SnakeAidDbContext db, Guid userId, decimal balance)
    {
        await SeedAccountAsync(db, userId, AccountRole.User);
        return await SeedWalletAsync(db, userId, balance);
    }

    private static async Task SeedAccountAsync(SnakeAidDbContext db, Guid userId, AccountRole role)
    {
        if (await db.Users.AnyAsync(u => u.Id == userId))
        {
            return;
        }

        db.Users.Add(new Account
        {
            Id = userId,
            UserName = $"user.{userId:N}",
            NormalizedUserName = $"USER.{userId:N}".ToUpperInvariant(),
            Email = $"{userId:N}@example.com",
            NormalizedEmail = $"{userId:N}@example.com".ToUpperInvariant(),
            FullName = $"User {userId:N}",
            Role = role,
            IsActive = true
        });
        await db.SaveChangesAsync();
    }

    private static async Task<Wallet> SeedWalletAsync(SnakeAidDbContext db, Guid userId, decimal balance)
    {
        var wallet = new Wallet
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Balance = balance
        };

        db.Wallets.Add(wallet);
        await db.SaveChangesAsync();
        return wallet;
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

    private sealed class WalletWithdrawSqliteDbContext : SnakeAidDbContext
    {
        public WalletWithdrawSqliteDbContext(DbContextOptions<SnakeAidDbContext> options)
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
