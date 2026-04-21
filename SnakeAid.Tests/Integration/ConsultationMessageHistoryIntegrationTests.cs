using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Requests.Consultation;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Implements;
using SnakeAid.Service.Implements;
using SnakeAid.Service.Interfaces;
using System.Reflection;

namespace SnakeAid.Tests.Integration;

public class ConsultationMessageHistoryIntegrationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SnakeAidDbContext _db;
    private readonly ConsultationService _service;

    private readonly Guid _memberId = Guid.NewGuid();
    private readonly Guid _expertId = Guid.NewGuid();
    private readonly Guid _adminId = Guid.NewGuid();
    private readonly Guid _outsiderId = Guid.NewGuid();
    private readonly Guid _completedConsultationId = Guid.NewGuid();
    private readonly Guid _cancelledConsultationId = Guid.NewGuid();
    private readonly Guid _expertAbsentConsultationId = Guid.NewGuid();
    private readonly Guid _ongoingConsultationId = Guid.NewGuid();

    public ConsultationMessageHistoryIntegrationTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<SnakeAidDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new ConsultationMessageHistorySqliteDbContext(options);
        _db.Database.EnsureCreated();

        SeedData();

        _service = new ConsultationService(
            new UnitOfWork<SnakeAidDbContext>(_db),
            new FakeConsultationPaymentService(),
            NullLogger<ConsultationService>.Instance);
    }

    [Fact]
    public async Task GetConsultationMessageHistoryAsync_Participant_ShouldReturnNewestPageAscending()
    {
        var result = await _service.GetConsultationMessageHistoryAsync(
            _completedConsultationId,
            _memberId,
            false,
            new ConsultationMessageHistoryQueryRequest
            {
                PageNumber = 1,
                PageSize = 2
            });

        var items = result.Items.ToList();

        Assert.Equal(2, items.Count);
        Assert.Equal("msg-4", items[0].Content);
        Assert.Equal("msg-5", items[1].Content);
        Assert.Equal(1, result.Meta.CurrentPage);
        Assert.Equal(3, result.Meta.TotalPages);
        Assert.True(items[0].SentAt <= items[1].SentAt);
    }

    [Fact]
    public async Task GetConsultationMessageHistoryAsync_NextPage_ShouldReturnNextOlderBatchAscending()
    {
        var result = await _service.GetConsultationMessageHistoryAsync(
            _completedConsultationId,
            _memberId,
            false,
            new ConsultationMessageHistoryQueryRequest
            {
                PageNumber = 2,
                PageSize = 2
            });

        var items = result.Items.ToList();

        Assert.Equal(2, items.Count);
        Assert.Equal("msg-2", items[0].Content);
        Assert.Equal("msg-3", items[1].Content);
        Assert.True(items[0].SentAt <= items[1].SentAt);
    }

    [Fact]
    public async Task GetConsultationMessageHistoryAsync_Admin_ShouldBypassParticipantCheck()
    {
        var result = await _service.GetConsultationMessageHistoryAsync(
            _completedConsultationId,
            _adminId,
            true,
            new ConsultationMessageHistoryQueryRequest
            {
                PageNumber = 1,
                PageSize = 2
            });

        Assert.Equal(2, result.Items.Count());
    }

    [Fact]
    public async Task GetConsultationMessageHistoryAsync_CancelledConsultation_ShouldReturnHistory()
    {
        var result = await _service.GetConsultationMessageHistoryAsync(
            _cancelledConsultationId,
            _memberId,
            false,
            new ConsultationMessageHistoryQueryRequest
            {
                PageNumber = 1,
                PageSize = 10
            });

        var item = Assert.Single(result.Items);
        Assert.Equal("cancelled-msg-1", item.Content);
    }

    [Fact]
    public async Task GetConsultationMessageHistoryAsync_NonParticipantNonAdmin_ShouldThrowForbiddenException()
    {
        await Assert.ThrowsAsync<ForbiddenException>(() => _service.GetConsultationMessageHistoryAsync(
            _completedConsultationId,
            _outsiderId,
            false,
            new ConsultationMessageHistoryQueryRequest
            {
                PageNumber = 1,
                PageSize = 2
            }));
    }

    [Fact]
    public async Task GetConsultationMessageHistoryAsync_NonTerminalConsultation_ShouldThrowBusinessException()
    {
        await Assert.ThrowsAsync<BusinessException>(() => _service.GetConsultationMessageHistoryAsync(
            _ongoingConsultationId,
            _memberId,
            false,
            new ConsultationMessageHistoryQueryRequest
            {
                PageNumber = 1,
                PageSize = 2
            }));
    }

    [Fact]
    public async Task GetConsultationMessageHistoryAsync_AttachmentOnlyMessage_ShouldPreserveStoredTruth()
    {
        var result = await _service.GetConsultationMessageHistoryAsync(
            _expertAbsentConsultationId,
            _memberId,
            false,
            new ConsultationMessageHistoryQueryRequest
            {
                PageNumber = 1,
                PageSize = 10
            });

        var item = Assert.Single(result.Items);
        Assert.Equal(string.Empty, item.Content);
        Assert.NotNull(item.AttachmentUrl);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private void SeedData()
    {
        _db.Set<Account>().AddRange(
            CreateAccount(_memberId, "Member One", AccountRole.User),
            CreateAccount(_expertId, "Expert One", AccountRole.Expert),
            CreateAccount(_adminId, "Admin One", AccountRole.Admin),
            CreateAccount(_outsiderId, "Outsider One", AccountRole.User));

        _db.Set<Consultation>().AddRange(
            new Consultation
            {
                Id = _completedConsultationId,
                CallerId = _memberId,
                CalleeId = _expertId,
                RoomId = "room-completed",
                StartTime = DateTime.UtcNow.AddHours(-2),
                EndTime = DateTime.UtcNow.AddHours(-1),
                Status = ConsultationStatus.Completed,
                Type = ConsultationType.Scheduled
            },
            new Consultation
            {
                Id = _expertAbsentConsultationId,
                CallerId = _memberId,
                CalleeId = _expertId,
                RoomId = "room-expert-absent",
                StartTime = DateTime.UtcNow.AddHours(-3),
                EndTime = DateTime.UtcNow.AddHours(-2),
                Status = ConsultationStatus.ExpertAbsent,
                Type = ConsultationType.Scheduled
            },
            new Consultation
            {
                Id = _cancelledConsultationId,
                CallerId = _memberId,
                CalleeId = _expertId,
                RoomId = "room-cancelled",
                StartTime = DateTime.UtcNow.AddHours(-4),
                EndTime = DateTime.UtcNow.AddHours(-3),
                Status = ConsultationStatus.Cancelled,
                Type = ConsultationType.Scheduled
            },
            new Consultation
            {
                Id = _ongoingConsultationId,
                CallerId = _memberId,
                CalleeId = _expertId,
                RoomId = "room-ongoing",
                StartTime = DateTime.UtcNow.AddMinutes(-30),
                EndTime = null,
                Status = ConsultationStatus.Ongoing,
                Type = ConsultationType.Scheduled
            });

        var baseTime = DateTime.UtcNow.AddHours(-2);
        _db.Set<ChatMessage>().AddRange(
            new ChatMessage
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                ConsultationId = _completedConsultationId,
                SenderId = _memberId,
                Content = "msg-1",
                SentAt = baseTime.AddMinutes(1)
            },
            new ChatMessage
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                ConsultationId = _completedConsultationId,
                SenderId = _expertId,
                Content = "msg-2",
                SentAt = baseTime.AddMinutes(2)
            },
            new ChatMessage
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                ConsultationId = _completedConsultationId,
                SenderId = _memberId,
                Content = "msg-3",
                SentAt = baseTime.AddMinutes(3)
            },
            new ChatMessage
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                ConsultationId = _completedConsultationId,
                SenderId = _expertId,
                Content = "msg-4",
                SentAt = baseTime.AddMinutes(4)
            },
            new ChatMessage
            {
                Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                ConsultationId = _completedConsultationId,
                SenderId = _memberId,
                Content = "msg-5",
                SentAt = baseTime.AddMinutes(5)
            },
            new ChatMessage
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                ConsultationId = _expertAbsentConsultationId,
                SenderId = _expertId,
                Content = string.Empty,
                AttachmentUrl = "https://example.com/attachment.jpg",
                SentAt = baseTime.AddMinutes(10)
            },
            new ChatMessage
            {
                Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                ConsultationId = _cancelledConsultationId,
                SenderId = _memberId,
                Content = "cancelled-msg-1",
                SentAt = baseTime.AddMinutes(11)
            });

        _db.SaveChanges();
    }

    private static Account CreateAccount(Guid id, string fullName, AccountRole role) =>
        new()
        {
            Id = id,
            FullName = fullName,
            UserName = $"{fullName.Replace(" ", ".").ToLowerInvariant()}.{id:N}",
            NormalizedUserName = $"{fullName.Replace(" ", ".").ToUpperInvariant()}.{id:N}",
            Email = $"{id:N}@test.local",
            NormalizedEmail = $"{id:N}@TEST.LOCAL",
            IsActive = true,
            Role = role
        };

    private sealed class ConsultationMessageHistorySqliteDbContext : SnakeAidDbContext
    {
        public ConsultationMessageHistorySqliteDbContext(DbContextOptions<SnakeAidDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var keep = new HashSet<Type>
            {
                typeof(Account),
                typeof(Consultation),
                typeof(ChatMessage)
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
                {
                    modelBuilder.Ignore(type);
                }
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

            modelBuilder.Entity<ChatMessage>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Content)
                    .HasMaxLength(2000);
                entity.HasOne(c => c.Consultation)
                    .WithMany()
                    .HasForeignKey(c => c.ConsultationId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(c => c.Sender)
                    .WithMany()
                    .HasForeignKey(c => c.SenderId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }

    private sealed class FakeConsultationPaymentService : IConsultationPaymentService
    {
        public Task<Core.Responses.Consultation.ConsultationPaymentResponse> ConfirmConsultationPaymentAsync(Guid transactionId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Core.Responses.PayOs.PayOsWebhookResponse> ConfirmConsultationPaymentByOrderCodeAsync(long orderCode, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Core.Responses.Consultation.ConsultationPaymentResponse> PayScheduledBookingAsync(Guid userId, Guid bookingId, ProcessConsultationPaymentRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Core.Responses.Consultation.ConsultationPaymentResponse> PayEmergencyRequestAsync(Guid userId, Guid requestId, ProcessConsultationPaymentRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> IsConsultationPayOsOrderCodeAsync(long orderCode, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<Core.Responses.PayOs.PayOsWebhookResponse> ProcessConsultationWebhookAsync(string rawPayload, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> RefundEmergencyEscrowAsync(Guid requestId, string reason, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<bool> RefundScheduledBookingAsync(Guid bookingId, Guid receiverId, string reason, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<bool> CancelPendingScheduledBookingPaymentAsync(Guid bookingId, string reason, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<int> ExpireEmergencyRequestsAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<bool> SettleConsultationEscrowAsync(Guid consultationId, CancellationToken cancellationToken = default) => Task.FromResult(true);
    }
}
