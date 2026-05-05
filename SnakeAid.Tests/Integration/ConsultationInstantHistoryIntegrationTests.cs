using System.Reflection;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Requests.Consultation;
using SnakeAid.Core.Responses.Consultation;
using SnakeAid.Core.Responses.Consultation.History;
using SnakeAid.Core.Responses.PayOs;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Implements;
using SnakeAid.Service.Implements;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Tests.Integration;

public class ConsultationInstantHistoryIntegrationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SnakeAidDbContext _db;
    private readonly ConsultationService _service;

    private readonly Guid _memberId = Guid.NewGuid();
    private readonly Guid _expertId = Guid.NewGuid();
    private readonly Guid _acceptedConsultationId = Guid.NewGuid();
    private readonly Guid _acceptedRequestId = Guid.NewGuid();
    private readonly Guid _declinedRequestId = Guid.NewGuid();
    private readonly Guid _expiredRequestId = Guid.NewGuid();

    private static readonly DateTime AcceptedAt = new(2026, 5, 5, 1, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime DeclinedAt = new(2026, 5, 5, 2, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime ExpiredAt = new(2026, 5, 5, 3, 0, 0, DateTimeKind.Utc);

    public ConsultationInstantHistoryIntegrationTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<SnakeAidDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new InstantHistorySqliteDbContext(options);
        _db.Database.EnsureCreated();
        SeedData();

        _service = new ConsultationService(
            new UnitOfWork<SnakeAidDbContext>(_db),
            new FakeConsultationPaymentService(),
            NullLogger<ConsultationService>.Instance);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task MemberHistory_IncludesDeclinedAndExpiredInstantRows_WhenStatusOmitted()
    {
        var result = await _service.GetMyConsultationsAsync(_memberId, new MyConsultationsQueryRequest
        {
            PageNumber = 1,
            PageSize = 10,
            Type = "Emergency"
        });

        var instantRows = result.Items.OfType<MyInstantConsultationRequestHistoryResponse>().ToList();
        var consultationRow = Assert.Single(result.Items.OfType<MyConsultationHistoryResponse>());

        Assert.Equal(_acceptedConsultationId, consultationRow.ConsultationId);
        Assert.Equal("consultation", consultationRow.Kind);
        Assert.Equal("instant", Assert.Single(instantRows, r => r.InstantRequestId == _declinedRequestId).Kind);
        Assert.Equal("DeclinedByExpert", Assert.Single(instantRows, r => r.InstantRequestId == _declinedRequestId).RequestStatus);
        Assert.Equal("Expired", Assert.Single(instantRows, r => r.InstantRequestId == _expiredRequestId).RequestStatus);
        Assert.Equal(new[] { _expiredRequestId, _declinedRequestId, _acceptedRequestId },
            result.Items.Select(item => item.Kind == "instant"
                ? ((MyInstantConsultationRequestHistoryResponse)item).InstantRequestId
                : ((MyConsultationHistoryResponse)item).EmergencyRequestId!.Value));
    }

    [Fact]
    public async Task ExpertHistory_IncludesDeclinedAndExpiredInstantRows_WhenStatusOmitted()
    {
        var result = await _service.GetExpertConsultationsAsync(_expertId, new MyConsultationsQueryRequest
        {
            PageNumber = 1,
            PageSize = 10,
            Type = "Emergency"
        });

        var instantRows = result.Items.OfType<ExpertInstantConsultationRequestHistoryResponse>().ToList();
        var consultationRow = Assert.Single(result.Items.OfType<ExpertConsultationHistoryResponse>());

        Assert.Equal(_acceptedConsultationId, consultationRow.ConsultationId);
        Assert.Equal(_memberId, Assert.Single(instantRows, r => r.InstantRequestId == _declinedRequestId).UserId);
        Assert.Equal("DeclinedByExpert", Assert.Single(instantRows, r => r.InstantRequestId == _declinedRequestId).RequestStatus);
        Assert.Equal("Expired", Assert.Single(instantRows, r => r.InstantRequestId == _expiredRequestId).RequestStatus);
    }

    [Fact]
    public async Task StatusFilter_ReturnsOnlyConsultationRows()
    {
        var result = await _service.GetMyConsultationsAsync(_memberId, new MyConsultationsQueryRequest
        {
            PageNumber = 1,
            PageSize = 10,
            Type = "Emergency",
            Status = "Completed"
        });

        var row = Assert.Single(result.Items.OfType<MyConsultationHistoryResponse>());
        Assert.Equal(_acceptedConsultationId, row.ConsultationId);
        Assert.Empty(result.Items.OfType<MyInstantConsultationRequestHistoryResponse>());
    }

    [Fact]
    public void PolymorphicSerialization_WritesDerivedInstantFields()
    {
        MyConsultationHistoryUnionResponse item = new MyInstantConsultationRequestHistoryResponse
        {
            InstantRequestId = _declinedRequestId,
            Type = "Emergency",
            RequestStatus = "DeclinedByExpert",
            RequestedAt = DeclinedAt.AddMinutes(-2),
            RespondedAt = DeclinedAt,
            ExpertId = _expertId,
            ExpertName = "Expert One"
        };

        var json = JsonSerializer.Serialize(item, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.Contains("\"kind\":\"instant\"", json);
        Assert.Contains("\"instantRequestId\"", json);
        Assert.Contains("\"requestStatus\":\"DeclinedByExpert\"", json);
        Assert.DoesNotContain("consultationId", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExpertAbsentDto_RemainsSeparateFromHistoryUnion()
    {
        Assert.False(typeof(MyConsultationResponse).IsAssignableTo(typeof(MyConsultationHistoryUnionResponse)));
        Assert.DoesNotContain("Kind", typeof(MyConsultationResponse).GetProperties().Select(p => p.Name));
        Assert.DoesNotContain("InstantRequestId", typeof(MyConsultationResponse).GetProperties().Select(p => p.Name));
    }

    private void SeedData()
    {
        _db.Set<Account>().AddRange(
            new Account { Id = _memberId, FullName = "Member One", AvatarUrl = "member.png" },
            new Account { Id = _expertId, FullName = "Expert One", AvatarUrl = "expert.png" });

        _db.Set<Consultation>().Add(new Consultation
        {
            Id = _acceptedConsultationId,
            CallerId = _memberId,
            CalleeId = _expertId,
            RoomId = "room-accepted",
            StartTime = AcceptedAt,
            EndTime = AcceptedAt.AddMinutes(30),
            Status = ConsultationStatus.Completed,
            Type = ConsultationType.Emergency
        });

        _db.Set<ConsultationPingRequest>().AddRange(
            new ConsultationPingRequest
            {
                Id = _acceptedRequestId,
                RescuerId = _memberId,
                ExpertId = _expertId,
                Status = ConsultationPingStatus.AcceptedByExpert,
                RequestedAt = AcceptedAt.AddMinutes(-1),
                RespondedAt = AcceptedAt,
                ConsultationId = _acceptedConsultationId
            },
            new ConsultationPingRequest
            {
                Id = _declinedRequestId,
                RescuerId = _memberId,
                ExpertId = _expertId,
                Status = ConsultationPingStatus.DeclinedByExpert,
                RequestedAt = DeclinedAt.AddMinutes(-2),
                RespondedAt = DeclinedAt
            },
            new ConsultationPingRequest
            {
                Id = _expiredRequestId,
                RescuerId = _memberId,
                ExpertId = _expertId,
                Status = ConsultationPingStatus.Expired,
                RequestedAt = ExpiredAt.AddMinutes(-2),
                RespondedAt = ExpiredAt
            });

        _db.SaveChanges();
    }

    private sealed class InstantHistorySqliteDbContext : SnakeAidDbContext
    {
        public InstantHistorySqliteDbContext(DbContextOptions<SnakeAidDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var keep = new HashSet<Type>
            {
                typeof(Account),
                typeof(Consultation),
                typeof(ConsultationPingRequest),
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
                {
                    modelBuilder.Ignore(type);
                }
            }

            modelBuilder.Entity<Account>().HasKey(a => a.Id);
            modelBuilder.Entity<Consultation>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.HasOne(c => c.Caller).WithMany().HasForeignKey(c => c.CallerId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(c => c.Callee).WithMany().HasForeignKey(c => c.CalleeId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<ConsultationPingRequest>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Status).HasConversion<int>().IsRequired();
                entity.HasOne(p => p.Rescuer).WithMany().HasForeignKey(p => p.RescuerId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(p => p.Expert).WithMany().HasForeignKey(p => p.ExpertId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(p => p.Consultation).WithMany().HasForeignKey(p => p.ConsultationId).OnDelete(DeleteBehavior.SetNull);
                entity.Ignore(p => p.RescueMission);
            });
            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.HasOne(t => t.User).WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Restrict);
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
