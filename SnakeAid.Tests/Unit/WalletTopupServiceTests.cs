using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Settings;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Implements;
using SnakeAid.Service.Implements;
using SnakeAid.Service.Interfaces;
using SnakeAid.Service.Services.PayOs.Models;

namespace SnakeAid.Tests.Unit;

public class WalletTopupServiceTests
{
    [Fact]
    public async Task ProcessWalletTopupWebhook_CreditsOnlyTransactionOwnerWallet()
    {
        const string rawPayload = "{\"data\":{}}";
        const long orderCode = 123456;

        await using var db = CreateDbContext();
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();

        await SeedAccountAsync(db, ownerId, "owner");
        await SeedAccountAsync(db, otherUserId, "other");

        db.Set<Wallet>().Add(new Wallet
        {
            Id = Guid.NewGuid(),
            UserId = ownerId,
            Balance = 100000
        });
        db.Set<Wallet>().Add(new Wallet
        {
            Id = Guid.NewGuid(),
            UserId = otherUserId,
            Balance = 50000
        });
        db.Set<Transaction>().Add(new Transaction
        {
            Id = transactionId,
            UserId = ownerId,
            ReferenceId = ownerId,
            Amount = 200000,
            Currency = "VND",
            TransactionType = TransactionType.WalletTopup,
            Description = $"TOPUP-{orderCode}",
            PaymentMethod = "PayOS",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var paymentGateway = new Mock<IPaymentGateway>();
        paymentGateway.Setup(g => g.VerifyWebhook(rawPayload))
            .Returns(new PayOsWebhookData
            {
                Success = true,
                Code = "00",
                Description = $"TOPUP-{orderCode}",
                OrderCode = orderCode,
                Amount = 200000,
                TransactionReference = "PAYOS-TOPUP-REF",
                TransactionDateTime = DateTime.UtcNow
            });

        var service = new WalletTopupService(
            paymentGateway.Object,
            new UnitOfWork<SnakeAidDbContext>(db),
            Options.Create(new PayOsOptions()),
            NullLogger<WalletTopupService>.Instance);

        var firstResult = await service.ProcessWalletTopupWebhookAsync(rawPayload, CancellationToken.None);
        var secondResult = await service.ProcessWalletTopupWebhookAsync(rawPayload, CancellationToken.None);

        Assert.True(firstResult.Success);
        Assert.True(secondResult.Success);

        var ownerWallet = await db.Set<Wallet>().SingleAsync(w => w.UserId == ownerId);
        var otherWallet = await db.Set<Wallet>().SingleAsync(w => w.UserId == otherUserId);
        var transaction = await db.Set<Transaction>().SingleAsync(t => t.Id == transactionId);

        Assert.Equal(300000, ownerWallet.Balance);
        Assert.Equal(50000, otherWallet.Balance);
        Assert.Equal("PAYOS-TOPUP-REF", transaction.ExternalTransactionId);
    }

    private static SnakeAidDbContext CreateDbContext()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<SnakeAidDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new WalletTopupSqliteDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static async Task SeedAccountAsync(SnakeAidDbContext db, Guid userId, string tag)
    {
        db.Set<Account>().Add(new Account
        {
            Id = userId,
            UserName = $"{tag}-{userId:N}",
            Email = $"{tag}-{userId:N}@example.com",
            PasswordHash = "hashed",
            FullName = $"{tag} user",
            Role = AccountRole.User,
            IsActive = true
        });

        await db.SaveChangesAsync();
    }

    private sealed class WalletTopupSqliteDbContext : SnakeAidDbContext
    {
        public WalletTopupSqliteDbContext(DbContextOptions<SnakeAidDbContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var keep = new HashSet<Type>
            {
                typeof(Account),
                typeof(Wallet),
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
