using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Requests.Consultation;
using SnakeAid.Core.Responses.Consultation;
using SnakeAid.Core.Responses.PayOs;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Implements;
using SnakeAid.Service.Implements;
using SnakeAid.Service.Interfaces;
using System.Reflection;

namespace SnakeAid.Tests.Integration;

/// <summary>
/// Bug condition exploration test for emergency consultation price.
/// This test encodes the EXPECTED (correct) behavior.
/// It is expected to FAIL on unfixed code — failure confirms the bug exists.
///
/// Bug Condition: GetMyConsultationsAsync never queries the Transaction table
/// for emergency consultations, so Price is always null even when a matching
/// Transaction (TransactionType = ConsultationPayment, ReferenceId = PingRequest.Id) exists.
///
/// **Validates: Requirements 1.1, 1.2, 2.1**
/// </summary>
public class ConsultationPriceBugConditionTests
{
    /// <summary>
    /// Property 1: Bug Condition — Emergency Consultation Price Is Null Despite Matching Transaction.
    ///
    /// Setup: user with one emergency consultation that has a matching Transaction
    /// (TransactionType = ConsultationPayment, ReferenceId = ConsultationPingRequest.Id, Amount = 200000).
    ///
    /// Expected: MyConsultationResponse.Price == 200000.
    /// Actual (unfixed): MyConsultationResponse.Price == null.
    ///
    /// **Validates: Requirements 1.1, 1.2, 2.1**
    /// </summary>
    [Fact]
    public async Task GetMyConsultations_EmergencyWithTransaction_ShouldReturnPrice()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expertId = Guid.NewGuid();
        var pingRequestId = Guid.NewGuid();
        var consultationId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        const decimal expectedPrice = 200_000m;

        await using var db = CreateDbContext();

        // Seed user
        db.Set<Account>().Add(new Account
        {
            Id = userId,
            FullName = "Test User",
            UserName = $"user.{userId:N}",
            NormalizedUserName = $"USER.{userId:N}",
            Email = $"user.{userId:N}@test.local",
            NormalizedEmail = $"USER.{userId:N}@TEST.LOCAL",
            IsActive = true,
            Role = AccountRole.User
        });

        // Seed expert
        db.Set<Account>().Add(new Account
        {
            Id = expertId,
            FullName = "Test Expert",
            UserName = $"expert.{expertId:N}",
            NormalizedUserName = $"EXPERT.{expertId:N}",
            Email = $"expert.{expertId:N}@test.local",
            NormalizedEmail = $"EXPERT.{expertId:N}@TEST.LOCAL",
            IsActive = true,
            Role = AccountRole.Expert
        });

        // Seed consultation
        db.Consultations.Add(new Consultation
        {
            Id = consultationId,
            CallerId = userId,
            CalleeId = expertId,
            RoomId = "room-test-123",
            StartTime = DateTime.UtcNow.AddHours(-1),
            EndTime = DateTime.UtcNow,
            Status = ConsultationStatus.Completed,
            Type = ConsultationType.Emergency
        });

        // Seed ping request (AcceptedByExpert, linked to consultation)
        db.ConsultationPingRequests.Add(new ConsultationPingRequest
        {
            Id = pingRequestId,
            RescuerId = userId,
            ExpertId = expertId,
            Status = ConsultationPingStatus.AcceptedByExpert,
            RequestedAt = DateTime.UtcNow.AddHours(-1),
            ConsultationId = consultationId
        });

        // Seed transaction (ConsultationPayment, ReferenceId = ping request Id)
        db.Transactions.Add(new Transaction
        {
            Id = transactionId,
            UserId = userId,
            ReferenceId = pingRequestId,
            Amount = expectedPrice,
            TransactionType = TransactionType.ConsultationPayment,
            Description = "Emergency consultation payment",
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        });

        await db.SaveChangesAsync();

        var unitOfWork = new UnitOfWork<SnakeAidDbContext>(db);
        var service = new ConsultationService(
            unitOfWork,
            new FakeConsultationPaymentService(),
            NullLogger<ConsultationService>.Instance);

        var query = new MyConsultationsQueryRequest
        {
            PageNumber = 1,
            PageSize = 10
            // No type filter — returns both scheduled and emergency
        };

        // Act
        var result = await service.GetMyConsultationsAsync(userId, query);

        // Assert
        var emergencyConsultation = result.Items.FirstOrDefault(c => c.Type == "Emergency");
        Assert.NotNull(emergencyConsultation);
        Assert.Equal(pingRequestId, emergencyConsultation.EmergencyRequestId);

        // This assertion encodes the EXPECTED behavior.
        // On UNFIXED code, Price will be null — this test should FAIL.
        Assert.Equal(expectedPrice, emergencyConsultation.Price);
    }

    #region Helpers

    private static SnakeAidDbContext CreateDbContext()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<SnakeAidDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new ConsultationPriceSqliteDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    /// <summary>
    /// Minimal SQLite DbContext that only keeps the entities needed for this test.
    /// </summary>
    private sealed class ConsultationPriceSqliteDbContext : SnakeAidDbContext
    {
        public ConsultationPriceSqliteDbContext(DbContextOptions<SnakeAidDbContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var keep = new HashSet<Type>
            {
                typeof(Account),
                typeof(Consultation),
                typeof(ConsultationBooking),
                typeof(ConsultationPingRequest),
                typeof(ExpertTimeSlot),
                typeof(Transaction)
            };

            var dbSetEntityTypes = typeof(SnakeAidDbContext)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType.IsGenericType
                         && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
                .Select(p => p.PropertyType.GetGenericArguments()[0])
                .Distinct();

            foreach (var type in dbSetEntityTypes)
            {
                if (!keep.Contains(type))
                    modelBuilder.Ignore(type);
            }

            modelBuilder.Entity<Account>(entity =>
            {
                entity.HasKey(a => a.Id);
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

            modelBuilder.Entity<ExpertTimeSlot>(entity =>
            {
                entity.HasKey(s => s.Id);
                entity.Property(s => s.Version)
                    .IsConcurrencyToken()
                    .ValueGeneratedNever()
                    .HasDefaultValue(0u);
                entity.HasOne(s => s.Expert)
                    .WithMany()
                    .HasForeignKey(s => s.ExpertId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ConsultationPingRequest>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Status)
                    .HasConversion<int>()
                    .IsRequired();
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

            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.TransactionType).HasConversion<int>();
                entity.HasOne(t => t.User)
                    .WithMany()
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }

    private sealed class FakeConsultationPaymentService : IConsultationPaymentService
    {
        public Task<ConsultationPaymentResponse> ConfirmConsultationPaymentAsync(Guid transactionId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PayOsWebhookResponse> ConfirmConsultationPaymentByOrderCodeAsync(long orderCode, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ConsultationPaymentResponse> PayScheduledBookingAsync(Guid userId, Guid bookingId, ProcessConsultationPaymentRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ConsultationPaymentResponse> PayEmergencyRequestAsync(Guid userId, Guid requestId, ProcessConsultationPaymentRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> IsConsultationPayOsOrderCodeAsync(long orderCode, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<PayOsWebhookResponse> ProcessConsultationWebhookAsync(string rawPayload, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> RefundEmergencyEscrowAsync(Guid requestId, string reason, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<bool> RefundScheduledBookingAsync(Guid bookingId, Guid receiverId, string reason, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<bool> CancelPendingScheduledBookingPaymentAsync(Guid bookingId, string reason, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<int> ExpireEmergencyRequestsAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<bool> SettleConsultationEscrowAsync(Guid consultationId, CancellationToken cancellationToken = default) => Task.FromResult(true);
    }

    #endregion
}
