using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Requests.Consultation;
using SnakeAid.Core.Requests.LiveKit;
using SnakeAid.Core.Responses.Consultation;
using SnakeAid.Core.Responses.LiveKit;
using SnakeAid.Core.Responses.PayOs;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Hubs;
using SnakeAid.Service.Implements;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Tests.Unit;

public class BookingServiceConcurrencyTests
{
    [Fact]
    public async Task CreateScheduledBookingAsync_ShouldThrowConflictException_WhenDbConcurrencyOccurs()
    {
        var service = new BookingService(
            new ConcurrencyThrowingUnitOfWork(),
            new NoOpConsultationPaymentService(),
            new NoOpHubContext(),
            new NoOpLiveKitService(),
            NullLogger<BookingService>.Instance);
        var request = new CreateConsultationBookingRequest { TimeSlotId = Guid.NewGuid() };

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateScheduledBookingAsync(Guid.NewGuid(), request));
    }

    private sealed class ConcurrencyThrowingUnitOfWork : IUnitOfWork<SnakeAidDbContext>
    {
        public SnakeAidDbContext Context => throw new NotImplementedException();

        public IGenericRepository<TEntity> GetRepository<TEntity>() where TEntity : class
            => throw new NotImplementedException();

        public Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation)
            => throw new DbUpdateConcurrencyException("Simulated concurrency conflict.");

        public Task ExecuteInTransactionAsync(Func<Task> operation)
            => throw new DbUpdateConcurrencyException("Simulated concurrency conflict.");

        public int Commit() => throw new NotImplementedException();
        public Task<int> CommitAsync() => throw new NotImplementedException();
        public Task RollbackAsync() => throw new NotImplementedException();
        public void ClearChangeTracker() => throw new NotImplementedException();
        public void Dispose() { }
    }

    private sealed class NoOpConsultationPaymentService : IConsultationPaymentService
    {
        public Task<ConsultationPaymentResponse> ConfirmConsultationPaymentAsync(Guid transactionId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PayOsWebhookResponse> ConfirmConsultationPaymentByOrderCodeAsync(long orderCode, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<int> ExpireEmergencyRequestsAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<bool> IsConsultationPayOsOrderCodeAsync(long orderCode, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<ConsultationPaymentResponse> PayEmergencyRequestAsync(Guid userId, Guid requestId, ProcessConsultationPaymentRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ConsultationPaymentResponse> PayScheduledBookingAsync(Guid userId, Guid bookingId, ProcessConsultationPaymentRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PayOsWebhookResponse> ProcessConsultationWebhookAsync(string rawPayload, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> RefundEmergencyEscrowAsync(Guid requestId, string reason, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> SettleConsultationEscrowAsync(Guid consultationId, CancellationToken cancellationToken = default) => Task.FromResult(false);
    }

    private sealed class NoOpHubContext : IHubContext<ConsultationHub>
    {
        public IHubClients Clients { get; } = new NoOpHubClients();
        public IGroupManager Groups => throw new NotImplementedException();

        private sealed class NoOpHubClients : IHubClients
        {
            private readonly IClientProxy _proxy = new NoOpClientProxy();
            public IClientProxy All => _proxy;
            public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => _proxy;
            public IClientProxy Client(string connectionId) => _proxy;
            public IClientProxy Clients(IReadOnlyList<string> connectionIds) => _proxy;
            public IClientProxy Group(string groupName) => _proxy;
            public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => _proxy;
            public IClientProxy Groups(IReadOnlyList<string> groupNames) => _proxy;
            public IClientProxy User(string userId) => _proxy;
            public IClientProxy Users(IReadOnlyList<string> userIds) => _proxy;
        }

        private sealed class NoOpClientProxy : IClientProxy
        {
            public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
                => Task.CompletedTask;
        }
    }

    private sealed class NoOpLiveKitService : ILiveKitService
    {
        public string GenerateAccessToken(string identity, string roomName, VideoGrants grants, string? metadata = null, TimeSpan? ttl = null)
            => string.Empty;
        public Task<RoomInfoResponse> CreateRoomAsync(string roomName, int maxParticipants = 2, int emptyTimeoutSeconds = 600, CancellationToken cancellationToken = default)
            => Task.FromResult(new RoomInfoResponse());
        public Task DeleteRoomAsync(string roomName, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
        public Task<List<RoomInfoResponse>> ListRoomsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new List<RoomInfoResponse>());
        public LiveKitWebhookPayload? ValidateWebhook(string body, string authorizationHeader)
            => null;
    }
}
