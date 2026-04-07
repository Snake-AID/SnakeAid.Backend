using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Requests.Consultation;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Implements;
using SnakeAid.Service.Implements;
using SnakeAid.Service.Interfaces;
using SnakeAid.Service.Services.PayOs.Models;
using System.Reflection;

namespace SnakeAid.Tests.Integration;

public class ConsultationPaymentIntegrationTests
{
    private static readonly Guid SystemUserId = Guid.Parse("57288b98-5f91-4de8-b827-866e3df69587");

    [Fact]
    public async Task PayScheduledBookingAsync_ShouldMoveFundsToEscrow_AndConfirmBooking()
    {
        var userId = Guid.NewGuid();
        var expertId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var consultationId = Guid.NewGuid();
        var slotId = Guid.NewGuid();

        await using var db = CreateDbContext();
        await SeedAccountsAsync(db, userId, expertId);
        await SeedWalletsAsync(db, userId, expertId, 500_000m, 0m, 0m);

        db.ExpertTimeSlots.Add(new ExpertTimeSlot
        {
            Id = slotId,
            ExpertId = expertId,
            StartTime = DateTime.UtcNow.AddHours(1),
            EndTime = DateTime.UtcNow.AddHours(1.5),
            Status = TimeSlotStatus.Reserved,
            Version = 0
        });

        db.Consultations.Add(new Consultation
        {
            Id = consultationId,
            CallerId = userId,
            CalleeId = expertId,
            RoomId = $"consultation-{consultationId:N}",
            StartTime = DateTime.UtcNow.AddHours(1),
            Status = ConsultationStatus.Scheduled,
            Type = ConsultationType.Scheduled
        });

        db.ConsultationBookings.Add(new ConsultationBooking
        {
            Id = bookingId,
            UserId = userId,
            ExpertId = expertId,
            Price = 150_000m,
            BookedAt = DateTime.UtcNow,
            PaymentDeadline = DateTime.UtcNow.AddMinutes(15),
            Status = BookingStatus.PendingPayment,
            TimeSlotId = slotId,
            ConsultationId = consultationId
        });

        await db.SaveChangesAsync();

        var service = CreatePaymentService(db);
        var response = await service.PayScheduledBookingAsync(userId, bookingId, new ProcessConsultationPaymentRequest
        {
            PaymentMethod = ConsultationPaymentMethod.WalletBalance
        });

        Assert.Equal(SnakeAid.Core.Responses.Consultation.ConsultationPaymentReferenceType.ScheduledBooking, response.ReferenceType);
        Assert.Equal(150_000m, response.Amount);

        var booking = await db.ConsultationBookings.FirstAsync(x => x.Id == bookingId);
        Assert.Equal(BookingStatus.Confirmed, booking.Status);

        var userWallet = await db.Set<Wallet>().FirstAsync(w => w.UserId == userId);
        var systemWallet = await db.Set<Wallet>().FirstAsync(w => w.UserId == SystemUserId);
        Assert.Equal(350_000m, userWallet.Balance);
        Assert.Equal(150_000m, systemWallet.Balance);

        var paymentTx = await db.Set<Transaction>().FirstAsync(t => t.ReferenceId == bookingId && t.TransactionType == TransactionType.ConsultationPayment);
        Assert.Equal(150_000m, paymentTx.Amount);

        var escrowCreditTx = await db.Set<Transaction>().FirstAsync(t =>
            t.ReferenceId == bookingId &&
            t.TransactionType == TransactionType.EscrowHold);
        Assert.Equal(SystemUserId, escrowCreditTx.UserId);
        Assert.Equal(150_000m, escrowCreditTx.Amount);
        Assert.False(await db.Set<Transaction>().AnyAsync(t =>
            t.ReferenceId == bookingId &&
            t.UserId == SystemUserId &&
            t.TransactionType == TransactionType.WalletTopup));
    }

    [Fact]
    public async Task PayEmergencyRequestAsync_ShouldMoveFundsToEscrow_AndActivateRequest()
    {
        var userId = Guid.NewGuid();
        var expertId = Guid.NewGuid();
        var requestId = Guid.NewGuid();

        await using var db = CreateDbContext();
        await SeedAccountsAsync(db, userId, expertId);
        await SeedWalletsAsync(db, userId, expertId, 500_000m, 0m, 0m);

        db.ExpertProfiles.Add(new ExpertProfile
        {
            AccountId = expertId,
            Biography = "Emergency expert",
            ConsultationFee = 150_000m,
            EmergencyConsultationFee = 500_000m,
            IsOnline = true
        });

        db.ConsultationPingRequests.Add(new ConsultationPingRequest
        {
            Id = requestId,
            RescuerId = userId,
            ExpertId = expertId,
            RequestedAt = DateTime.UtcNow,
            Status = ConsultationPingStatus.PendingPayment
        });

        await db.SaveChangesAsync();

        var notificationService = new RecordingExpertEmergencyNotificationService();
        var service = CreatePaymentService(db, notificationService);
        var response = await service.PayEmergencyRequestAsync(userId, requestId, new ProcessConsultationPaymentRequest
        {
            PaymentMethod = ConsultationPaymentMethod.WalletBalance
        });

        Assert.Equal(SnakeAid.Core.Responses.Consultation.ConsultationPaymentReferenceType.EmergencyRequest, response.ReferenceType);
        Assert.Equal(500_000m, response.Amount);
        Assert.Single(notificationService.PushedEmergencyRequestIds);
        Assert.Equal(requestId, notificationService.PushedEmergencyRequestIds[0]);

        var ping = await db.ConsultationPingRequests.FirstAsync(x => x.Id == requestId);
        Assert.Equal(ConsultationPingStatus.PendingExpertResponse, ping.Status);
        Assert.NotNull(ping.ExpiresAt);

        var userWallet = await db.Set<Wallet>().FirstAsync(w => w.UserId == userId);
        var systemWallet = await db.Set<Wallet>().FirstAsync(w => w.UserId == SystemUserId);
        Assert.Equal(0m, userWallet.Balance);
        Assert.Equal(500_000m, systemWallet.Balance);
    }

    [Fact]
    public async Task PayScheduledBookingAsync_WithPayOs_ShouldCreatePendingIntent_WithoutEscrowingImmediately()
    {
        var userId = Guid.NewGuid();
        var expertId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var consultationId = Guid.NewGuid();
        var slotId = Guid.NewGuid();

        await using var db = CreateDbContext();
        await SeedAccountsAsync(db, userId, expertId);
        await SeedWalletsAsync(db, userId, expertId, 500_000m, 0m, 0m);

        db.ExpertTimeSlots.Add(new ExpertTimeSlot
        {
            Id = slotId,
            ExpertId = expertId,
            StartTime = DateTime.UtcNow.AddHours(1),
            EndTime = DateTime.UtcNow.AddHours(1.5),
            Status = TimeSlotStatus.Reserved,
            Version = 0
        });

        db.Consultations.Add(new Consultation
        {
            Id = consultationId,
            CallerId = userId,
            CalleeId = expertId,
            RoomId = $"consultation-{consultationId:N}",
            StartTime = DateTime.UtcNow.AddHours(1),
            Status = ConsultationStatus.Scheduled,
            Type = ConsultationType.Scheduled
        });

        db.ConsultationBookings.Add(new ConsultationBooking
        {
            Id = bookingId,
            UserId = userId,
            ExpertId = expertId,
            Price = 150_000m,
            BookedAt = DateTime.UtcNow,
            PaymentDeadline = DateTime.UtcNow.AddMinutes(15),
            Status = BookingStatus.PendingPayment,
            TimeSlotId = slotId,
            ConsultationId = consultationId
        });

        await db.SaveChangesAsync();

        var service = CreatePaymentService(db, paymentGateway: new FakePaymentGateway());
        var response = await service.PayScheduledBookingAsync(userId, bookingId, new ProcessConsultationPaymentRequest
        {
            PaymentMethod = ConsultationPaymentMethod.PayOs
        });

        Assert.Equal("Pending", response.Status);
        Assert.Equal(ConsultationPaymentMethod.PayOs, response.PaymentMethod);
        Assert.NotNull(response.CheckoutUrl);
        Assert.NotNull(response.OrderCode);
        Assert.InRange(response.OrderCode!.Value, 1_000_000_000_100L, 99_999_999_999_999L);

        var paymentTx = await db.Set<Transaction>().FirstAsync(t => t.Id == response.TransactionId);
        Assert.True(paymentTx.Description.Length <= 25);

        var booking = await db.ConsultationBookings.FirstAsync(x => x.Id == bookingId);
        Assert.Equal(BookingStatus.PendingPayment, booking.Status);

        var userWallet = await db.Set<Wallet>().FirstAsync(w => w.UserId == userId);
        var systemWallet = await db.Set<Wallet>().FirstAsync(w => w.UserId == SystemUserId);
        Assert.Equal(500_000m, userWallet.Balance);
        Assert.Equal(0m, systemWallet.Balance);
    }

    [Fact]
    public async Task ConfirmConsultationPaymentAsync_WithPayOs_ShouldEscrowAndConfirmBooking()
    {
        var userId = Guid.NewGuid();
        var expertId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var consultationId = Guid.NewGuid();
        var slotId = Guid.NewGuid();

        await using var db = CreateDbContext();
        await SeedAccountsAsync(db, userId, expertId);
        await SeedWalletsAsync(db, userId, expertId, 500_000m, 0m, 0m);

        db.ExpertTimeSlots.Add(new ExpertTimeSlot
        {
            Id = slotId,
            ExpertId = expertId,
            StartTime = DateTime.UtcNow.AddHours(1),
            EndTime = DateTime.UtcNow.AddHours(1.5),
            Status = TimeSlotStatus.Reserved,
            Version = 0
        });

        db.Consultations.Add(new Consultation
        {
            Id = consultationId,
            CallerId = userId,
            CalleeId = expertId,
            RoomId = $"consultation-{consultationId:N}",
            StartTime = DateTime.UtcNow.AddHours(1),
            Status = ConsultationStatus.Scheduled,
            Type = ConsultationType.Scheduled
        });

        db.ConsultationBookings.Add(new ConsultationBooking
        {
            Id = bookingId,
            UserId = userId,
            ExpertId = expertId,
            Price = 150_000m,
            BookedAt = DateTime.UtcNow,
            PaymentDeadline = DateTime.UtcNow.AddMinutes(15),
            Status = BookingStatus.PendingPayment,
            TimeSlotId = slotId,
            ConsultationId = consultationId
        });

        await db.SaveChangesAsync();

        var paymentGateway = new FakePaymentGateway();
        var service = CreatePaymentService(db, paymentGateway: paymentGateway);
        var pending = await service.PayScheduledBookingAsync(userId, bookingId, new ProcessConsultationPaymentRequest
        {
            PaymentMethod = ConsultationPaymentMethod.PayOs
        });

        var confirmed = await service.ConfirmConsultationPaymentAsync(pending.TransactionId);

        Assert.Equal("Escrowed", confirmed.Status);
        Assert.Equal(ConsultationPaymentMethod.PayOs, confirmed.PaymentMethod);

        var booking = await db.ConsultationBookings.FirstAsync(x => x.Id == bookingId);
        Assert.Equal(BookingStatus.Confirmed, booking.Status);

        var paymentTx = await db.Set<Transaction>().FirstAsync(t => t.Id == pending.TransactionId);
        Assert.False(string.IsNullOrWhiteSpace(paymentTx.ExternalTransactionId));

        var systemWallet = await db.Set<Wallet>().FirstAsync(w => w.UserId == SystemUserId);
        Assert.Equal(150_000m, systemWallet.Balance);
    }

    [Fact]
    public async Task RejectEmergencyRequestAsync_ShouldRefundEscrowToUser()
    {
        var userId = Guid.NewGuid();
        var expertId = Guid.NewGuid();
        var requestId = Guid.NewGuid();

        await using var db = CreateDbContext();
        await SeedAccountsAsync(db, userId, expertId);
        await SeedWalletsAsync(db, userId, expertId, 0m, 0m, 500_000m);

        db.ConsultationPingRequests.Add(new ConsultationPingRequest
        {
            Id = requestId,
            RescuerId = userId,
            ExpertId = expertId,
            RequestedAt = DateTime.UtcNow.AddMinutes(-1),
            ExpiresAt = DateTime.UtcNow.AddMinutes(1),
            Status = ConsultationPingStatus.PendingExpertResponse
        });

        db.Set<Transaction>().Add(new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ReferenceId = requestId,
            Amount = 500_000m,
            Currency = "VND",
            TransactionType = TransactionType.ConsultationPayment,
            PaymentMethod = "Wallet",
            ExternalTransactionId = "seed-payment",
            Description = "Emergency consultation payment",
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var paymentService = CreatePaymentService(db);
        var emergencyService = new EmergencyConsultationService(
            new UnitOfWork<SnakeAidDbContext>(db),
            new RecordingExpertEmergencyNotificationService(),
            paymentService,
            NullLogger<EmergencyConsultationService>.Instance);

        var response = await emergencyService.RejectEmergencyRequestAsync(requestId, expertId);

        Assert.Equal(ConsultationPingStatus.DeclinedByExpert, response.Status);

        var userWallet = await db.Set<Wallet>().FirstAsync(w => w.UserId == userId);
        var systemWallet = await db.Set<Wallet>().FirstAsync(w => w.UserId == SystemUserId);
        Assert.Equal(500_000m, userWallet.Balance);
        Assert.Equal(0m, systemWallet.Balance);

        Assert.NotNull(await db.Set<Transaction>().FirstOrDefaultAsync(t => t.ReferenceId == requestId && t.TransactionType == TransactionType.ConsultationRefund));
        Assert.NotNull(await db.Set<Transaction>().FirstOrDefaultAsync(t => t.ReferenceId == requestId && t.TransactionType == TransactionType.EscrowRelease));
        Assert.False(await db.Set<Transaction>().AnyAsync(t =>
            t.ReferenceId == requestId &&
            t.UserId == SystemUserId &&
            t.TransactionType == TransactionType.WalletWithdraw));
    }

    [Fact]
    public async Task SettleConsultationEscrowAsync_ShouldBeIdempotent()
    {
        var userId = Guid.NewGuid();
        var expertId = Guid.NewGuid();
        var consultationId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var slotId = Guid.NewGuid();

        await using var db = CreateDbContext();
        await SeedAccountsAsync(db, userId, expertId);
        await SeedWalletsAsync(db, userId, expertId, 0m, 0m, 150_000m);

        db.ExpertTimeSlots.Add(new ExpertTimeSlot
        {
            Id = slotId,
            ExpertId = expertId,
            StartTime = DateTime.UtcNow.AddHours(-1),
            EndTime = DateTime.UtcNow.AddMinutes(-30),
            Status = TimeSlotStatus.Booked,
            Version = 0
        });

        db.Consultations.Add(new Consultation
        {
            Id = consultationId,
            CallerId = userId,
            CalleeId = expertId,
            RoomId = $"consultation-{consultationId:N}",
            StartTime = DateTime.UtcNow.AddHours(-1),
            EndTime = DateTime.UtcNow.AddMinutes(-30),
            Status = ConsultationStatus.Completed,
            Type = ConsultationType.Scheduled
        });

        db.ConsultationBookings.Add(new ConsultationBooking
        {
            Id = bookingId,
            UserId = userId,
            ExpertId = expertId,
            Price = 150_000m,
            BookedAt = DateTime.UtcNow.AddHours(-2),
            PaymentDeadline = DateTime.UtcNow.AddHours(-2),
            Status = BookingStatus.Completed,
            TimeSlotId = slotId,
            ConsultationId = consultationId
        });

        db.Set<Transaction>().Add(new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ReferenceId = bookingId,
            Amount = 150_000m,
            Currency = "VND",
            TransactionType = TransactionType.ConsultationPayment,
            PaymentMethod = "Wallet",
            ExternalTransactionId = "seed-payment",
            Description = "Scheduled consultation payment",
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var service = CreatePaymentService(db);
        var first = await service.SettleConsultationEscrowAsync(consultationId);
        var second = await service.SettleConsultationEscrowAsync(consultationId);

        Assert.True(first);
        Assert.False(second);

        var systemWallet = await db.Set<Wallet>().FirstAsync(w => w.UserId == SystemUserId);
        var expertWallet = await db.Set<Wallet>().FirstAsync(w => w.UserId == expertId);
        Assert.Equal(0m, systemWallet.Balance);
        Assert.Equal(150_000m, expertWallet.Balance);
        Assert.Equal(1, await db.Set<Transaction>().CountAsync(t => t.ReferenceId == consultationId && t.TransactionType == TransactionType.ExpertPayout));
        Assert.Equal(1, await db.Set<Transaction>().CountAsync(t => t.ReferenceId == consultationId && t.TransactionType == TransactionType.EscrowRelease));
        Assert.Equal(0, await db.Set<Transaction>().CountAsync(t =>
            t.ReferenceId == consultationId &&
            t.UserId == SystemUserId &&
            t.TransactionType == TransactionType.WalletWithdraw));
    }

    private static ConsultationPaymentService CreatePaymentService(
        SnakeAidDbContext db,
        IExpertEmergencyNotificationService? notificationService = null,
        IPaymentGateway? paymentGateway = null)
    {
        return new ConsultationPaymentService(
            new UnitOfWork<SnakeAidDbContext>(db),
            notificationService ?? new RecordingExpertEmergencyNotificationService(),
            paymentGateway ?? new FakePaymentGateway(),
            NullLogger<ConsultationPaymentService>.Instance);
    }

    private static SnakeAidDbContext CreateDbContext()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<SnakeAidDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new ConsultationPaymentSqliteDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static async Task SeedAccountsAsync(SnakeAidDbContext db, Guid userId, Guid expertId)
    {
        db.Set<Account>().Add(new Account
        {
            Id = userId,
            FullName = "Payment User",
            UserName = $"payment.user.{userId:N}",
            NormalizedUserName = $"PAYMENT.USER.{userId:N}".ToUpperInvariant(),
            Email = $"payment.user.{userId:N}@test.local",
            NormalizedEmail = $"PAYMENT.USER.{userId:N}@TEST.LOCAL",
            IsActive = true,
            Role = AccountRole.User
        });

        db.Set<Account>().Add(new Account
        {
            Id = expertId,
            FullName = "Payment Expert",
            UserName = $"payment.expert.{expertId:N}",
            NormalizedUserName = $"PAYMENT.EXPERT.{expertId:N}".ToUpperInvariant(),
            Email = $"payment.expert.{expertId:N}@test.local",
            NormalizedEmail = $"PAYMENT.EXPERT.{expertId:N}@TEST.LOCAL",
            IsActive = true,
            Role = AccountRole.Expert
        });

        db.Set<Account>().Add(new Account
        {
            Id = SystemUserId,
            FullName = "System Wallet",
            UserName = "system.wallet",
            NormalizedUserName = "SYSTEM.WALLET",
            Email = "system.wallet@test.local",
            NormalizedEmail = "SYSTEM.WALLET@TEST.LOCAL",
            IsActive = true,
            Role = AccountRole.Admin
        });

        await db.SaveChangesAsync();
    }

    private static async Task SeedWalletsAsync(SnakeAidDbContext db, Guid userId, Guid expertId, decimal userBalance, decimal expertBalance, decimal systemBalance)
    {
        db.Set<Wallet>().Add(new Wallet
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Balance = userBalance
        });

        db.Set<Wallet>().Add(new Wallet
        {
            Id = Guid.NewGuid(),
            UserId = expertId,
            Balance = expertBalance
        });

        db.Set<Wallet>().Add(new Wallet
        {
            Id = Guid.NewGuid(),
            UserId = SystemUserId,
            Balance = systemBalance
        });

        await db.SaveChangesAsync();
    }

    private sealed class RecordingExpertEmergencyNotificationService : IExpertEmergencyNotificationService
    {
        public List<Guid> PushedEmergencyRequestIds { get; } = new();
        public bool IsExpertConnected(string expertId) => true;
        public Task SendEmergencyRequestAsync(string expertId, object requestData)
        {
            var requestId = (Guid)requestData.GetType().GetProperty("requestId")!.GetValue(requestData)!;
            PushedEmergencyRequestIds.Add(requestId);
            return Task.CompletedTask;
        }

        public Task NotifyEmergencyRequestStatusChangedAsync(Guid requestId, object statusData) => Task.CompletedTask;
    }

    private sealed class FakePaymentGateway : IPaymentGateway
    {
        public Task<PayOsPaymentLinkResult> CreatePaymentLinkAsync(PayOsCreatePaymentRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new PayOsPaymentLinkResult
            {
                Success = true,
                OrderCode = request.OrderCode,
                PaymentLinkId = $"link-{request.OrderCode}",
                CheckoutUrl = $"https://payos.test/{request.OrderCode}",
                Amount = request.Amount,
                Status = "PENDING",
                Currency = "VND"
            });

        public Task<PayOsPaymentLinkResult> CancelPaymentLinkAsync(long orderCode, string? cancellationReason, CancellationToken cancellationToken)
            => Task.FromResult(new PayOsPaymentLinkResult
            {
                Success = true,
                OrderCode = orderCode,
                Status = "CANCELLED"
            });

        public Task<PayOsLinkInformation?> GetPaymentLinkInformationAsync(long orderCode, CancellationToken cancellationToken)
            => Task.FromResult<PayOsLinkInformation?>(new PayOsLinkInformation
            {
                Id = $"link-{orderCode}",
                OrderCode = orderCode,
                Amount = 150_000,
                AmountPaid = 150_000,
                AmountRemaining = 0,
                Status = "PAID"
            });

        public PayOsWebhookData VerifyWebhook(string rawPayload) => throw new NotImplementedException();
    }

    private sealed class ConsultationPaymentSqliteDbContext : SnakeAidDbContext
    {
        public ConsultationPaymentSqliteDbContext(DbContextOptions<SnakeAidDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var keep = new HashSet<Type>
            {
                typeof(Account),
                typeof(ExpertProfile),
                typeof(ExpertTimeSlot),
                typeof(Consultation),
                typeof(ConsultationBooking),
                typeof(ConsultationPingRequest),
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

            modelBuilder.Entity<ExpertProfile>(entity =>
            {
                entity.HasKey(e => e.AccountId);
                entity.HasOne(e => e.Account)
                    .WithOne()
                    .HasForeignKey<ExpertProfile>(e => e.AccountId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.Ignore(e => e.Specializations);
            });

            modelBuilder.Entity<ExpertTimeSlot>(entity =>
            {
                entity.HasKey(s => s.Id);
                entity.Property(s => s.Version)
                    .IsConcurrencyToken()
                    .ValueGeneratedNever()
                    .HasDefaultValue(0u);
            });

            modelBuilder.Entity<Consultation>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.HasOne(c => c.Caller)
                    .WithMany()
                    .HasForeignKey(c => c.CallerId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(c => c.Callee)
                    .WithMany()
                    .HasForeignKey(c => c.CalleeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ConsultationBooking>(entity =>
            {
                entity.HasKey(b => b.Id);
                entity.Property(b => b.Version)
                    .IsConcurrencyToken()
                    .ValueGeneratedNever()
                    .HasDefaultValue(0u);
                entity.HasOne(b => b.User)
                    .WithMany()
                    .HasForeignKey(b => b.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(b => b.Expert)
                    .WithMany()
                    .HasForeignKey(b => b.ExpertId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(b => b.TimeSlot)
                    .WithMany()
                    .HasForeignKey(b => b.TimeSlotId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(b => b.Consultation)
                    .WithMany()
                    .HasForeignKey(b => b.ConsultationId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<ConsultationPingRequest>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.HasOne(p => p.Rescuer)
                    .WithMany()
                    .HasForeignKey(p => p.RescuerId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(p => p.Expert)
                    .WithMany()
                    .HasForeignKey(p => p.ExpertId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(p => p.Consultation)
                    .WithMany()
                    .HasForeignKey(p => p.ConsultationId)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.Ignore(p => p.RescueMission);
            });

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
