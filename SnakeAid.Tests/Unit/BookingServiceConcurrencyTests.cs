using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Requests.Consultation;
using SnakeAid.Core.Responses.Consultation;
using SnakeAid.Core.Responses.PayOs;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Implements;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Tests.Unit;

public class BookingServiceConcurrencyTests
{
    [Fact]
    public async Task CreateScheduledBookingAsync_ShouldThrowConflictException_WhenDbConcurrencyOccurs()
    {
        var service = new BookingService(new ConcurrencyThrowingUnitOfWork(), new NoOpConsultationPaymentService(), NullLogger<BookingService>.Instance);
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
}
