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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SnakeAid.Tests.Integration;

public class EmergencyConsultationIntegrationTests
{
    [Fact]
    public async Task CreateEmergencyRequestAsync_ShouldCreatePendingRequest()
    {
        var userId = Guid.NewGuid();
        var expertId = Guid.NewGuid();

        await using var db = CreateDbContext();
        await SeedUserAndExpertAsync(db, userId, expertId, expertOnline: true);

        var service = new EmergencyConsultationService(
            new UnitOfWork<SnakeAidDbContext>(db),
            new FakeExpertEmergencyNotificationService(),
            new FakeConsultationPaymentService(),
            NullLogger<EmergencyConsultationService>.Instance);

        var response = await service.CreateEmergencyRequestAsync(userId, new CreateEmergencyConsultationRequest
        {
            ExpertId = expertId
        });

        Assert.Equal(expertId, response.ExpertId);
        Assert.Equal(userId, response.RequesterId);
        Assert.Equal(ConsultationPingStatus.PendingPayment, response.Status);

        var ping = await db.ConsultationPingRequests.FirstAsync(x => x.Id == response.RequestId);
        Assert.Equal(expertId, ping.ExpertId);
        Assert.Equal(userId, ping.RescuerId);
        Assert.NotNull(ping.ExpiresAt);
        Assert.True(ping.ExpiresAt > ping.RequestedAt);
    }

    [Fact]
    public async Task AcceptEmergencyRequestAsync_ShouldCreateConsultation_AndReserveOverlappingSlots()
    {
        var userId = Guid.NewGuid();
        var expertId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        await using var db = CreateDbContext();
        await SeedUserAndExpertAsync(db, userId, expertId, expertOnline: true);

        var overlapSlotId = Guid.NewGuid();
        var farSlotId = Guid.NewGuid();

        db.ExpertTimeSlots.Add(new ExpertTimeSlot
        {
            Id = overlapSlotId,
            ExpertId = expertId,
            StartTime = now.AddMinutes(5),
            EndTime = now.AddMinutes(35),
            Status = TimeSlotStatus.Available,
            Version = 0
        });

        db.ExpertTimeSlots.Add(new ExpertTimeSlot
        {
            Id = farSlotId,
            ExpertId = expertId,
            StartTime = now.AddMinutes(45),
            EndTime = now.AddMinutes(75),
            Status = TimeSlotStatus.Available,
            Version = 0
        });

        db.ConsultationPingRequests.Add(new ConsultationPingRequest
        {
            Id = requestId,
            RescuerId = userId,
            ExpertId = expertId,
            RequestedAt = now,
            ExpiresAt = now.AddMinutes(2),
            Status = ConsultationPingStatus.PendingExpertResponse
        });

        await db.SaveChangesAsync();

        var service = new EmergencyConsultationService(
            new UnitOfWork<SnakeAidDbContext>(db),
            new FakeExpertEmergencyNotificationService(),
            new FakeConsultationPaymentService(),
            NullLogger<EmergencyConsultationService>.Instance);

        var response = await service.AcceptEmergencyRequestAsync(requestId, expertId);

        Assert.Equal(ConsultationPingStatus.AcceptedByExpert, response.Status);
        Assert.NotNull(response.ConsultationId);
        Assert.False(string.IsNullOrWhiteSpace(response.RoomId));

        var consultation = await db.Consultations.FirstAsync(c => c.Id == response.ConsultationId!.Value);
        Assert.Equal(ConsultationStatus.Ongoing, consultation.Status);
        Assert.Equal(ConsultationType.Emergency, consultation.Type);
        Assert.Equal(userId, consultation.CallerId);
        Assert.Equal(expertId, consultation.CalleeId);

        var overlapSlot = await db.ExpertTimeSlots.FirstAsync(s => s.Id == overlapSlotId);
        var farSlot = await db.ExpertTimeSlots.FirstAsync(s => s.Id == farSlotId);
        Assert.Equal(TimeSlotStatus.Reserved, overlapSlot.Status);
        Assert.Equal(TimeSlotStatus.Available, farSlot.Status);
    }

    private static SnakeAidDbContext CreateDbContext()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<SnakeAidDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new EmergencyConsultationSqliteDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static async Task SeedUserAndExpertAsync(SnakeAidDbContext db, Guid userId, Guid expertId, bool expertOnline)
    {
        db.Set<Account>().Add(new Account
        {
            Id = userId,
            FullName = "Emergency User",
            UserName = $"user.{userId:N}",
            NormalizedUserName = $"USER.{userId:N}".ToUpperInvariant(),
            Email = $"user.{userId:N}@test.local",
            NormalizedEmail = $"USER.{userId:N}@TEST.LOCAL",
            IsActive = true,
            Role = AccountRole.User
        });

        db.Set<Account>().Add(new Account
        {
            Id = expertId,
            FullName = "Emergency Expert",
            UserName = $"expert.{expertId:N}",
            NormalizedUserName = $"EXPERT.{expertId:N}".ToUpperInvariant(),
            Email = $"expert.{expertId:N}@test.local",
            NormalizedEmail = $"EXPERT.{expertId:N}@TEST.LOCAL",
            IsActive = true,
            Role = AccountRole.Expert
        });

        db.ExpertProfiles.Add(new ExpertProfile
        {
            AccountId = expertId,
            Biography = "Emergency profile",
            ConsultationFee = 200_000m,
            IsOnline = expertOnline
        });

        await db.SaveChangesAsync();
    }

    private sealed class FakeExpertEmergencyNotificationService : IExpertEmergencyNotificationService
    {
        public bool IsExpertConnected(string expertId) => true;
        public Task SendEmergencyRequestAsync(string expertId, object requestData) => Task.CompletedTask;
        public Task NotifyEmergencyRequestStatusChangedAsync(Guid requestId, object statusData) => Task.CompletedTask;
    }

    private sealed class EmergencyConsultationSqliteDbContext : SnakeAidDbContext
    {
        public EmergencyConsultationSqliteDbContext(DbContextOptions<SnakeAidDbContext> options)
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
                typeof(ConsultationPingRequest)
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
                entity.Ignore(e => e.Specializations);
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
}
