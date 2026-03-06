using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Requests.Consultation;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Implements;

namespace SnakeAid.Tests.Unit;

public class BookingServiceConcurrencyTests
{
    [Fact]
    public async Task CreateScheduledBookingAsync_ShouldThrowConflictException_WhenDbConcurrencyOccurs()
    {
        var service = new BookingService(new ConcurrencyThrowingUnitOfWork(), NullLogger<BookingService>.Instance);
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
}
